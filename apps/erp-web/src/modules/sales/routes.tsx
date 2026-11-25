import type { RouteObject } from 'react-router';
import { collectionReportRoutes } from './reports/collection-report/routes';
import { customerDueRoutes } from './reports/customer-due/routes';
import { salesReportRoutes } from './reports/sales-report/routes';
import { salesCollectionRoutes } from './sales/sales-collection/routes';
import { salesDeliveryRoutes } from './sales/sales-delivery/routes';
import { salesOrderRoutes } from './sales/sales-order/routes';
import { salesQuotationRoutes } from './sales/sales-quotation/routes';
import { salesReturnRoutes } from './sales/sales-return/routes';

export const salesRoutes: RouteObject[] = [
  ...salesQuotationRoutes,
  ...salesOrderRoutes,
  ...salesDeliveryRoutes,
  ...salesReturnRoutes,
  ...salesCollectionRoutes,
  ...salesReportRoutes,
  ...collectionReportRoutes,
  ...customerDueRoutes,
];
