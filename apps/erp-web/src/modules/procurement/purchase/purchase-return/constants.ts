import { featurePath } from '@/common/constants/routes';
import { pathsOf, PROCUREMENT_LOOKUPS, PROCUREMENT_MODULE } from '../../procurement';

export const RETURN_FEATURE = 'procurement.purchase-return';
export const RETURN_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-return');

export const returnPaths = pathsOf(RETURN_PATH);

export const RETURN_LOOKUPS = {
  ...PROCUREMENT_LOOKUPS,
  reason: 'return-reason',
  receive: 'purchase-receive',
} as const;

export const RETURN_LOOKUP_LISTS = [RETURN_LOOKUPS.reason];

export const BATCH_MAX = 100;
