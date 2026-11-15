export const DRAFT_VIEW = 'draft';

export interface FormDraft<T> {
  continuing: boolean;
  onSave: (values: T) => Promise<unknown>;
}

export interface DraftItem {
  id: string;
  title: string;
  savedBy: string | null;
  savedAt: string;
}

export interface ListDrafts {
  rows?: DraftItem[];
  loading?: boolean;
  canEdit: boolean;
  onContinue: (draft: DraftItem) => void;
  onDelete: (draft: DraftItem) => Promise<unknown>;
}
