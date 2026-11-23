import type { RouteObject } from 'react-router';
import { REQUISITION_PATH } from './constants';

export const purchaseRequisitionRoutes: RouteObject[] = [
  {
    path: REQUISITION_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/RequisitionListPage')).RequisitionListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/RequisitionFormPage')).RequisitionFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/RequisitionViewPage')).RequisitionViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/RequisitionFormPage')).RequisitionFormPage,
        }),
      },
    ],
  },
];
