import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo } from 'react';
import {
  createDraft,
  createPanelDraft,
  deleteDraft,
  deletePanelDraft,
  getDraft,
  getGetDraftQueryKey,
  getGetPanelDraftQueryKey,
  getListDraftsQueryKey,
  getListPanelDraftsQueryKey,
  getPanelDraft,
  listDrafts,
  listPanelDrafts,
  updateDraft,
  updatePanelDraft,
} from '../generated/endpoints';
import type { DraftRow } from '../generated/model';

const API = {
  erp: {
    list: listDrafts,
    listKey: getListDraftsQueryKey,
    get: getDraft,
    getKey: getGetDraftQueryKey,
    create: createDraft,
    update: updateDraft,
    remove: deleteDraft,
  },
  panel: {
    list: listPanelDrafts,
    listKey: getListPanelDraftsQueryKey,
    get: getPanelDraft,
    getKey: getGetPanelDraftQueryKey,
    create: createPanelDraft,
    update: updatePanelDraft,
    remove: deletePanelDraft,
  },
};

export type DraftSurface = keyof typeof API;

const DRAFT_TITLE_KEYS = ['name', 'fullName', 'title', 'code'];
const SECRET_KEY = /password/i;

const toJson = (values: object) =>
  JSON.stringify(values, (key, value: unknown) => (SECRET_KEY.test(key) ? undefined : value));

const titleOf = (values: object): string => {
  const record = values as Record<string, unknown>;
  const key = DRAFT_TITLE_KEYS.find((k) => typeof record[k] === 'string' && record[k]);
  if (key) return String(record[key]).trim();
  const nested = Object.values(record).filter(
    (value): value is object => !!value && typeof value === 'object' && !Array.isArray(value),
  );
  return nested.map(titleOf).find(Boolean) ?? '';
};

interface DraftScope {
  surface: DraftSurface;
  feature: string;
  scopeKey?: unknown;
}

export function useDraftRows({ surface, feature, scopeKey }: DraftScope, enabled = true) {
  const api = API[surface];
  const client = useQueryClient();
  const queryKey = [...api.listKey({ feature }), scopeKey];
  const query = useQuery({ queryKey, queryFn: () => api.list({ feature }), enabled });

  return {
    rows: query.data?.map((row: DraftRow) => ({ ...row, savedBy: row.savedBy ?? null })),
    loading: query.isFetching,
    remove: async (id: string) => {
      await api.remove(id);
      await client.invalidateQueries({ queryKey: api.listKey({ feature }) });
    },
  };
}

export function useDraftValues<T extends object>(
  { surface, feature, scopeKey }: DraftScope,
  draftId: string | null,
  setDraftId: (id: string | null) => void,
) {
  const api = API[surface];
  const client = useQueryClient();
  const query = useQuery({
    queryKey: [...api.getKey(draftId ?? ''), scopeKey],
    queryFn: () => api.get(draftId ?? ''),
    enabled: !!draftId,
  });
  const values = useMemo(
    () => (query.data ? (JSON.parse(query.data.data) as Partial<T>) : undefined),
    [query.data],
  );
  const refreshList = () => client.invalidateQueries({ queryKey: api.listKey({ feature }) });

  return {
    loading: !!draftId && query.isPending,
    values,
    draft: {
      continuing: !!draftId,
      onSave: async (form: T) => {
        const data = { feature, title: titleOf(form), data: toJson(form) };
        const saved = draftId ? await api.update(draftId, data) : await api.create(data);
        client.setQueryData([...api.getKey(saved.id), scopeKey], saved);
        setDraftId(saved.id);
        await refreshList();
      },
    },
    finish: async () => {
      if (!draftId) return;
      await api.remove(draftId);
      setDraftId(null);
      await refreshList();
    },
  };
}

export type DraftState<T extends object> = ReturnType<typeof useDraftValues<T>>;
