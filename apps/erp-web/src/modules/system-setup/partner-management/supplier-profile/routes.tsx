import type { RouteObject } from 'react-router';
import { SUPPLIER_PATH } from '../partner-management';

export const supplierProfileRoutes: RouteObject[] = [
  {
    path: SUPPLIER_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/SupplierListPage')).SupplierListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/SupplierFormPage')).SupplierFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/SupplierViewPage')).SupplierViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/SupplierFormPage')).SupplierFormPage,
        }),
      },
    ],
  },
];
