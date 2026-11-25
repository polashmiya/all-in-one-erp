import type { RouteObject } from 'react-router';
import { salesQuotationRoutes } from './sales/sales-quotation/routes';

export const salesRoutes: RouteObject[] = [...salesQuotationRoutes];
