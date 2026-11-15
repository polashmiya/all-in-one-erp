import { useDraftRows, useDraftValues } from '@erp/api-client';
import { useDraftParam, type ListDrafts } from '@erp/ui';

export function useFormDraft<T extends object>(feature: string, enabled = true) {
  const [draftId, setDraftId] = useDraftParam();
  return useDraftValues<T>({ surface: 'panel', feature }, enabled ? draftId : null, setDraftId);
}

export function useListDrafts(feature: string, onContinue: (draftId: string) => void): ListDrafts {
  const drafts = useDraftRows({ surface: 'panel', feature });
  return {
    rows: drafts.rows,
    loading: drafts.loading,
    canEdit: true,
    onContinue: (draft) => onContinue(draft.id),
    onDelete: (draft) => drafts.remove(draft.id),
  };
}

export function useModalDraft<T extends object>(
  feature: string,
  open: boolean,
  editing: boolean,
  draftId: string | null,
  setDraftId: (id: string | null) => void,
) {
  const saved = useDraftValues<T>(
    { surface: 'panel', feature },
    open && !editing ? draftId : null,
    setDraftId,
  );
  return {
    values: saved.values,
    draft: editing ? undefined : saved.draft,
    finish: saved.finish,
    close: (onClose: () => void) => {
      setDraftId(null);
      onClose();
    },
  };
}
