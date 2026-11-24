import type { RouteObject } from 'react-router';
import { STOCK_BALANCE_PATH } from './constants';

export const stockBalanceRoutes: RouteObject[] = [
  {
    path: STOCK_BALANCE_PATH.slice(1),
    lazy: async () => ({ Component: (await import('./pages/StockBalancePage')).StockBalancePage }),
  },
];
