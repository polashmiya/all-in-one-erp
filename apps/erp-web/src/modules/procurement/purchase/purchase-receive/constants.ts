import { featurePath } from '@/common/constants/routes';
import { pathsOf, PROCUREMENT_LOOKUPS, PROCUREMENT_MODULE } from '../../procurement';

export const RECEIVE_FEATURE = 'procurement.purchase-receive';
export const RECEIVE_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-receive');

export const receivePaths = pathsOf(RECEIVE_PATH);

export const RECEIVE_LOOKUPS = {
  ...PROCUREMENT_LOOKUPS,
  paymentType: 'payment-type',
  order: 'purchase-order',
} as const;

export const RECEIVE_LOOKUP_LISTS = [RECEIVE_LOOKUPS.paymentType];

export const BATCH_MAX = 100;
