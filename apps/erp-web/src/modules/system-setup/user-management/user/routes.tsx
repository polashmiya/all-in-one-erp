import type { RouteObject } from 'react-router';
import { USER_PATH } from '../user-management';

export const userRoutes: RouteObject[] = [
  {
    path: USER_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({ Component: (await import('./pages/UserListPage')).UserListPage }),
      },
      {
        path: 'new',
        lazy: async () => ({ Component: (await import('./pages/UserFormPage')).UserFormPage }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({ Component: (await import('./pages/UserFormPage')).UserFormPage }),
      },
    ],
  },
];
