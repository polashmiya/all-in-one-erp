import type { RouteObject } from 'react-router';

export const erpUsersRoutes: RouteObject[] = [
  {
    path: 'erp-users',
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/ErpUserListPage')).ErpUserListPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/ErpUserDetailPage')).ErpUserDetailPage,
        }),
      },
    ],
  },
];
