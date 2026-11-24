import { featurePath } from '@/common/constants/routes';
import { DOCUMENT_LOOKUPS, pathsOf } from '@/common/documents/documents';
import { INVENTORY_MODULE } from '../../inventory';

export const TRANSFER_FEATURE = 'inventory.stock-transfer';
export const TRANSFER_PATH = featurePath(INVENTORY_MODULE, 'stock-transfer');

export const transferPaths = pathsOf(TRANSFER_PATH);

export const TRANSFER_LOOKUPS = { ...DOCUMENT_LOOKUPS, office: 'office' } as const;

export const TRANSFER_SIDES = [
  { office: 'fromOfficeId', warehouse: 'fromWarehouseId' },
  { office: 'toOfficeId', warehouse: 'toWarehouseId' },
] as const;

export const LINE_REMARKS_MAX = 300;
