import type { RouteObject } from 'react-router';
import { USER_ROLE_PATH } from '../user-management';
import './user-role.css';

export const userRoleRoutes: RouteObject[] = [
  {
    path: USER_ROLE_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({ Component: (await import('./pages/RoleListPage')).RoleListPage }),
      },
      {
        path: 'new',
        lazy: async () => ({ Component: (await import('./pages/RoleFormPage')).RoleFormPage }),
      },
      {
        path: ':id',
        lazy: async () => ({ Component: (await import('./pages/RoleFormPage')).RoleFormPage }),
      },
    ],
  },
];
