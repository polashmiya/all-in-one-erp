import type { RouteObject } from 'react-router';
import { approvalRoutes } from './approval/routes';
import { dashboardRoutes } from './dashboard/routes';
import { procurementRoutes } from './procurement/routes';
import { systemSetupRoutes } from './system-setup/routes';

export const moduleRoutes: RouteObject[] = [
  ...dashboardRoutes,
  ...systemSetupRoutes,
  ...procurementRoutes,
  ...approvalRoutes,
];
