import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface TableSettings {
  hidden: string[];
}

interface TableSettingsState {
  tables: Record<string, Partial<TableSettings>>;
  update: (key: string, patch: Partial<TableSettings>) => void;
}

const defaults: TableSettings = { hidden: [] };

const useTableSettingsStore = create<TableSettingsState>()(
  persist(
    (set) => ({
      tables: {},
      update: (key, patch) =>
        set((s) => ({ tables: { ...s.tables, [key]: { ...s.tables[key], ...patch } } })),
    }),
    { name: 'erp.ui.tables' },
  ),
);

export function useTableSettings(key: string, defaultHidden: string[] = defaults.hidden) {
  const saved = useTableSettingsStore((s) => s.tables[key]);
  const update = useTableSettingsStore((s) => s.update);
  const settings = { ...defaults, hidden: defaultHidden, ...saved };
  return [settings, (patch: Partial<TableSettings>) => update(key, patch)] as const;
}
