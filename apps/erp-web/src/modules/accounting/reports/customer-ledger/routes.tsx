import type { RouteObject } from 'react-router';
import { CUSTOMER_LEDGER_PATH } from './constants';

export const customerLedgerRoutes: RouteObject[] = [
  {
    path: CUSTOMER_LEDGER_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/CustomerLedgerPage')).CustomerLedgerPage,
    }),
  },
];
