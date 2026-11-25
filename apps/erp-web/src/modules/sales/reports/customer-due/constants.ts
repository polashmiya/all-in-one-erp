import { featurePath } from '@/common/constants/routes';
import { SALES_MODULE } from '../../sales';

export const CUSTOMER_DUE_FEATURE = 'sales.customer-due';
export const CUSTOMER_DUE_PATH = featurePath(SALES_MODULE, 'customer-due');

export const CUSTOMER_DUE_VIEWS = ['customer', 'bill'] as const;
export type DueView = (typeof CUSTOMER_DUE_VIEWS)[number];
