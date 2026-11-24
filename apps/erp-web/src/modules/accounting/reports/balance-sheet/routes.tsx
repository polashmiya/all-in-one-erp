import type { RouteObject } from 'react-router';
import { BALANCE_SHEET_PATH } from './constants';

export const balanceSheetRoutes: RouteObject[] = [
  {
    path: BALANCE_SHEET_PATH.slice(1),
    lazy: async () => ({ Component: (await import('./pages/BalanceSheetPage')).BalanceSheetPage }),
  },
];
