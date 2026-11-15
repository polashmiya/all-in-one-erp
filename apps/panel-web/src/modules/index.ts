import type { RouteObject } from 'react-router';
import { accountsRoutes } from './accounts/routes';
import { erpUsersRoutes } from './erp-users/routes';

export const moduleRoutes: RouteObject[] = [...accountsRoutes, ...erpUsersRoutes];
