import type { RouteObject } from 'react-router';
import { purchaseRequisitionRoutes } from './purchase/purchase-requisition/routes';

export const procurementRoutes: RouteObject[] = [...purchaseRequisitionRoutes];
