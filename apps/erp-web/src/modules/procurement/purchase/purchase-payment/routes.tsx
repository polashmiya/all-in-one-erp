import type { RouteObject } from 'react-router';
import { PAYMENT_PATH } from './constants';

export const purchasePaymentRoutes: RouteObject[] = [
  {
    path: PAYMENT_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/PaymentListPage')).PaymentListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/PaymentFormPage')).PaymentFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/PaymentViewPage')).PaymentViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/PaymentFormPage')).PaymentFormPage,
        }),
      },
    ],
  },
];
