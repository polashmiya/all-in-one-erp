import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type ChartLayout = 'tree' | 'table';

interface ChartLayoutState {
  layout: ChartLayout;
  setLayout: (layout: ChartLayout) => void;
}

export const useChartLayout = create<ChartLayoutState>()(
  persist(
    (set) => ({
      layout: 'tree',
      setLayout: (layout) => set({ layout }),
    }),
    { name: 'erp.chart-of-accounts.layout' },
  ),
);
