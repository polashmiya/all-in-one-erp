import type { RouteObject } from 'react-router';
import { purchaseOrderRoutes } from './purchase/purchase-order/routes';
import { purchaseReceiveRoutes } from './purchase/purchase-receive/routes';
import { purchaseReturnRoutes } from './purchase/purchase-return/routes';
import { purchaseRequisitionRoutes } from './purchase/purchase-requisition/routes';

export const procurementRoutes: RouteObject[] = [
  ...purchaseRequisitionRoutes,
  ...purchaseOrderRoutes,
  ...purchaseReceiveRoutes,
  ...purchaseReturnRoutes,
];
