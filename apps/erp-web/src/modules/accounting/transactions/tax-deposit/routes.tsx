import type { RouteObject } from 'react-router';
import { DEPOSIT_PATH } from './constants';

export const taxDepositRoutes: RouteObject[] = [
  {
    path: DEPOSIT_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/DepositListPage')).DepositListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/DepositFormPage')).DepositFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/DepositViewPage')).DepositViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/DepositFormPage')).DepositFormPage,
        }),
      },
    ],
  },
];
