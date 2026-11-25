import type { RouteObject } from 'react-router';
import { salesDeliveryRoutes } from './sales/sales-delivery/routes';
import { salesOrderRoutes } from './sales/sales-order/routes';
import { salesQuotationRoutes } from './sales/sales-quotation/routes';

export const salesRoutes: RouteObject[] = [
  ...salesQuotationRoutes,
  ...salesOrderRoutes,
  ...salesDeliveryRoutes,
];
