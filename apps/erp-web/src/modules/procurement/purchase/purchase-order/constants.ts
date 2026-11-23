import { featurePath } from '@/common/constants/routes';
import { pathsOf, PROCUREMENT_LOOKUPS, PROCUREMENT_MODULE } from '../../procurement';

export const ORDER_FEATURE = 'procurement.purchase-order';
export const ORDER_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-order');

export const orderPaths = pathsOf(ORDER_PATH);

export const ORDER_LOOKUPS = {
  ...PROCUREMENT_LOOKUPS,
  paymentType: 'payment-type',
  requisition: 'purchase-requisition',
} as const;

export const ORDER_LOOKUP_LISTS = [ORDER_LOOKUPS.paymentType, ORDER_LOOKUPS.itemType];

export const SPECIFICATION_MAX = 1000;
export const VAT_MAX = 100;
