import { featurePath } from '@/common/constants/routes';
import { SALES_MODULE } from '../../sales';

export const SALES_REPORT_FEATURE = 'sales.sales-report';
export const SALES_REPORT_PATH = featurePath(SALES_MODULE, 'sales-report');

export const SALES_REPORT_VIEWS = [
  'delivery',
  'item',
  'category',
  'customer',
  'salesForce',
  'month',
] as const;
export type SalesView = (typeof SALES_REPORT_VIEWS)[number];
