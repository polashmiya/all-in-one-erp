import type { RouteObject } from 'react-router';
import { ADJUSTMENT_PATH } from './constants';

export const stockAdjustmentRoutes: RouteObject[] = [
  {
    path: ADJUSTMENT_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/AdjustmentListPage')).AdjustmentListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/AdjustmentFormPage')).AdjustmentFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/AdjustmentViewPage')).AdjustmentViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/AdjustmentFormPage')).AdjustmentFormPage,
        }),
      },
    ],
  },
];
