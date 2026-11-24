import type { RouteObject } from 'react-router';
import { TRANSFER_PATH } from './constants';

export const stockTransferRoutes: RouteObject[] = [
  {
    path: TRANSFER_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/TransferListPage')).TransferListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/TransferFormPage')).TransferFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/TransferViewPage')).TransferViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/TransferFormPage')).TransferFormPage,
        }),
      },
    ],
  },
];
