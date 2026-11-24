import type { RouteObject } from 'react-router';
import { approvalRoutes } from './approval/routes';
import { dashboardRoutes } from './dashboard/routes';
import { accountingRoutes } from './accounting/routes';
import { inventoryRoutes } from './inventory/routes';
import { procurementRoutes } from './procurement/routes';
import { systemSetupRoutes } from './system-setup/routes';

export const moduleRoutes: RouteObject[] = [
  ...dashboardRoutes,
  ...systemSetupRoutes,
  ...procurementRoutes,
  ...inventoryRoutes,
  ...accountingRoutes,
  ...approvalRoutes,
];
