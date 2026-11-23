import type { RouteObject } from 'react-router';
import { purchaseOrderRoutes } from './purchase/purchase-order/routes';
import { purchaseRequisitionRoutes } from './purchase/purchase-requisition/routes';

export const procurementRoutes: RouteObject[] = [
  ...purchaseRequisitionRoutes,
  ...purchaseOrderRoutes,
];
