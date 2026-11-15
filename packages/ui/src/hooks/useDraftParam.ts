import { useSearchParams } from 'react-router';

export const DRAFT_PARAM = 'draft';

export const withDraft = (path: string, draftId?: string) =>
  draftId ? `${path}?${DRAFT_PARAM}=${encodeURIComponent(draftId)}` : path;

export function useDraftParam() {
  const [params, setParams] = useSearchParams();
  const setDraftId = (id: string | null) =>
    setParams(
      (current) => {
        if (id) current.set(DRAFT_PARAM, id);
        else current.delete(DRAFT_PARAM);
        return current;
      },
      { replace: true },
    );
  return [params.get(DRAFT_PARAM), setDraftId] as const;
}
