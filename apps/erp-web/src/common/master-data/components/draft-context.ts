import { createContext } from 'react';

export const MasterDraftContext = createContext<{
  draftId: string | null;
  setDraftId: (id: string | null) => void;
}>({ draftId: null, setDraftId: () => undefined });
