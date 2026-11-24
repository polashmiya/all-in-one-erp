import type { RouteObject } from 'react-router';
import { BANK_PATH } from './constants';

export const bankAccountsRoutes: RouteObject[] = [
  {
    path: BANK_PATH.slice(1),
    lazy: async () => ({ Component: (await import('./pages/BankAccountsPage')).BankAccountsPage }),
  },
];
