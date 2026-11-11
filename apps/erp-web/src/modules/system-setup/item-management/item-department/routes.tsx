import type { RouteObject } from 'react-router';
import { itemManagementPaths } from '../item-management';

export const itemDepartmentRoutes: RouteObject[] = [
  {
    path: itemManagementPaths.department.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/ItemDepartmentPage')).ItemDepartmentPage,
    }),
  },
];
