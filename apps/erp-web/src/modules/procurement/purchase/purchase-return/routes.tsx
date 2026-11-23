import type { RouteObject } from 'react-router';
import { RETURN_PATH } from './constants';

export const purchaseReturnRoutes: RouteObject[] = [
  {
    path: RETURN_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/ReturnListPage')).ReturnListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/ReturnFormPage')).ReturnFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/ReturnViewPage')).ReturnViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/ReturnFormPage')).ReturnFormPage,
        }),
      },
    ],
  },
];
