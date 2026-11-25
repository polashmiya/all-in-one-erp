import type { RouteObject } from 'react-router';
import { SALES_REPORT_PATH } from './constants';

export const salesReportRoutes: RouteObject[] = [
  {
    path: SALES_REPORT_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/SalesReportPage')).SalesReportPage,
    }),
  },
];
