import { featurePath } from '@/common/constants/routes';
import { INVENTORY_MODULE } from '../../inventory';

export const STOCK_LEDGER_FEATURE = 'inventory.stock-ledger';
export const STOCK_LEDGER_PATH = featurePath(INVENTORY_MODULE, 'stock-ledger');

export const stockLedgerLink = (itemId: number, warehouseId: string) =>
  `${STOCK_LEDGER_PATH}?${new URLSearchParams({ itemId: String(itemId), warehouseId })}`;
