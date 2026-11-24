import type { RouteObject } from 'react-router';
import { STOCK_LEDGER_PATH } from './constants';

export const stockLedgerRoutes: RouteObject[] = [
  {
    path: STOCK_LEDGER_PATH.slice(1),
    lazy: async () => ({ Component: (await import('./pages/StockLedgerPage')).StockLedgerPage }),
  },
];
