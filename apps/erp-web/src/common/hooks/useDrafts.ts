import { useDraftRows, useDraftValues } from '@erp/api-client';
import { useDraftParam, type ListDrafts } from '@erp/ui';
import { useAuthStore } from '@/common/store/auth-store';

export function useFormDraft<T extends object>(feature: string, enabled = true) {
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const [draftId, setDraftId] = useDraftParam();
  return useDraftValues<T>(
    { surface: 'erp', feature, scopeKey: unitId },
    enabled ? draftId : null,
    setDraftId,
  );
}

export function useListDrafts(
  feature: string,
  canEdit: boolean,
  onContinue: (draftId: string) => void,
  enabled = true,
): ListDrafts {
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const drafts = useDraftRows({ surface: 'erp', feature, scopeKey: unitId }, enabled);
  return {
    rows: drafts.rows,
    loading: drafts.loading,
    canEdit,
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
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const saved = useDraftValues<T>(
    { surface: 'erp', feature, scopeKey: unitId },
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
