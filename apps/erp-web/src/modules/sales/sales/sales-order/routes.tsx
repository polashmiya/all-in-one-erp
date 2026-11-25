import type { RouteObject } from 'react-router';
import { ORDER_PATH } from './constants';

export const salesOrderRoutes: RouteObject[] = [
  {
    path: ORDER_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/OrderListPage')).OrderListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/OrderFormPage')).OrderFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/OrderViewPage')).OrderViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/OrderFormPage')).OrderFormPage,
        }),
      },
    ],
  },
];
