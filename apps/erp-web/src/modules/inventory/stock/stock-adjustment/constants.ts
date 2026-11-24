import { featurePath } from '@/common/constants/routes';
import { DOCUMENT_LOOKUPS, pathsOf } from '@/common/documents/documents';
import { INVENTORY_MODULE } from '../../inventory';

export const ADJUSTMENT_FEATURE = 'inventory.stock-adjustment';
export const ADJUSTMENT_PATH = featurePath(INVENTORY_MODULE, 'stock-adjustment');

export const adjustmentPaths = pathsOf(ADJUSTMENT_PATH);

export const ADJUSTMENT_LOOKUPS = {
  ...DOCUMENT_LOOKUPS,
  type: 'stock-adjustment-type',
  reason: 'stock-adjustment-reason',
} as const;

export const ADJUSTMENT_LOOKUP_LISTS = [ADJUSTMENT_LOOKUPS.reason];

export const STOCK_OUT = 'out';
export const ADJUSTMENT_TYPES = ['in', STOCK_OUT, 'opening'];

export const LINE_REMARKS_MAX = 300;
