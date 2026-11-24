import { featurePath } from '@/common/constants/routes';
import { INVENTORY_MODULE } from '../../inventory';

export const STOCK_BALANCE_FEATURE = 'inventory.stock-balance';
export const STOCK_BALANCE_PATH = featurePath(INVENTORY_MODULE, 'stock-balance');

export type StockView = 'all' | 'inStock' | 'outOfStock';
export const STOCK_VIEWS: StockView[] = ['all', 'inStock', 'outOfStock'];
