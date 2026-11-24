import type { RouteObject } from 'react-router';
import { purchaseOrderRoutes } from './purchase/purchase-order/routes';
import { purchasePaymentRoutes } from './purchase/purchase-payment/routes';
import { purchaseReceiveRoutes } from './purchase/purchase-receive/routes';
import { purchaseReturnRoutes } from './purchase/purchase-return/routes';
import { purchaseRequisitionRoutes } from './purchase/purchase-requisition/routes';
import { purchaseReportRoutes } from './reports/purchase-report/routes';

export const procurementRoutes: RouteObject[] = [
  ...purchaseRequisitionRoutes,
  ...purchaseOrderRoutes,
  ...purchaseReceiveRoutes,
  ...purchaseReturnRoutes,
  ...purchasePaymentRoutes,
  ...purchaseReportRoutes,
];
