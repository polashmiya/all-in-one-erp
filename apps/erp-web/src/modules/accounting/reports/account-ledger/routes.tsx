import type { RouteObject } from 'react-router';
import { ACCOUNT_LEDGER_PATH } from './constants';

export const accountLedgerRoutes: RouteObject[] = [
  {
    path: ACCOUNT_LEDGER_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/AccountLedgerPage')).AccountLedgerPage,
    }),
  },
];
