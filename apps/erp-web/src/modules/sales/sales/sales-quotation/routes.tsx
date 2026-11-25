import type { RouteObject } from 'react-router';
import { QUOTATION_PATH } from './constants';

export const salesQuotationRoutes: RouteObject[] = [
  {
    path: QUOTATION_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/QuotationListPage')).QuotationListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/QuotationFormPage')).QuotationFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/QuotationViewPage')).QuotationViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/QuotationFormPage')).QuotationFormPage,
        }),
      },
    ],
  },
];
