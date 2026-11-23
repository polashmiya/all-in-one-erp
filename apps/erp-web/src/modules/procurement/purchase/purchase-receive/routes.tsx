import type { RouteObject } from 'react-router';
import { RECEIVE_PATH } from './constants';

export const purchaseReceiveRoutes: RouteObject[] = [
  {
    path: RECEIVE_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/ReceiveListPage')).ReceiveListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/ReceiveFormPage')).ReceiveFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/ReceiveViewPage')).ReceiveViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/ReceiveFormPage')).ReceiveFormPage,
        }),
      },
    ],
  },
];
