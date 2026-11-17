import type { RouteObject } from 'react-router';
import { CUSTOMER_PATH } from '../partner-management';

export const customerProfileRoutes: RouteObject[] = [
  {
    path: CUSTOMER_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/CustomerListPage')).CustomerListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/CustomerFormPage')).CustomerFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/CustomerViewPage')).CustomerViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/CustomerFormPage')).CustomerFormPage,
        }),
      },
    ],
  },
];
