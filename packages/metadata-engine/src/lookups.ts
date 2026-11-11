import type { Language } from '@erp/ui';

export interface LookupItem {
  id: number;
  code?: string | null;
  name: string;
  nameBn?: string | null;
  isActive: boolean;
  parentId?: number | null;
}

export type LookupSources = Record<string, LookupItem[] | undefined>;

export const localized = (items: LookupItem[] | undefined, language: Language) =>
  language === 'bn'
    ? items?.map((item) => (item.nameBn ? { ...item, name: item.nameBn } : item))
    : items;

export const lookupLabel = (item: LookupItem) =>
  item.code ? `${item.name} (${item.code})` : item.name;

export function selectOptions(items: LookupItem[] | undefined, current?: unknown) {
  return (items ?? [])
    .filter((item) => item.isActive || item.id === current)
    .map((item) => ({ value: item.id, label: lookupLabel(item) }));
}

export function childSelectOptions(
  items: LookupItem[] | undefined,
  parents: LookupItem[] | undefined,
  parentId: unknown,
  current?: unknown,
) {
  const visible = (items ?? []).filter(
    (item) =>
      (item.isActive || item.id === current) && (parentId == null || item.parentId === parentId),
  );
  const toOption = (item: LookupItem) => ({ value: item.id, label: lookupLabel(item) });
  if (parentId != null) return visible.map(toOption);

  return (parents ?? [])
    .map((parent) => ({
      label: parent.name,
      title: parent.name,
      options: visible.filter((item) => item.parentId === parent.id).map(toOption),
    }))
    .filter((group) => group.options.length > 0);
}

export const parentOf = (items: LookupItem[] | undefined, id: unknown) =>
  id == null ? undefined : (items?.find((item) => item.id === id)?.parentId ?? undefined);
