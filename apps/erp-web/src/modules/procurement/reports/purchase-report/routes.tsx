import type { RouteObject } from 'react-router';
import { PURCHASE_REPORT_PATH } from './constants';

export const purchaseReportRoutes: RouteObject[] = [
  {
    path: PURCHASE_REPORT_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/PurchaseReportPage')).PurchaseReportPage,
    }),
  },
];
