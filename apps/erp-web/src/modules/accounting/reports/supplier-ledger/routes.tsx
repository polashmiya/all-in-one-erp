import type { RouteObject } from 'react-router';
import { SUPPLIER_LEDGER_PATH } from './constants';

export const supplierLedgerRoutes: RouteObject[] = [
  {
    path: SUPPLIER_LEDGER_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/SupplierLedgerPage')).SupplierLedgerPage,
    }),
  },
];
