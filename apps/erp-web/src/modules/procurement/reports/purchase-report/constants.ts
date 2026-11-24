import { featurePath } from '@/common/constants/routes';
import { PROCUREMENT_MODULE } from '../../procurement';

export const PURCHASE_REPORT_FEATURE = 'procurement.purchase-report';
export const PURCHASE_REPORT_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-report');

export type PurchaseView = 'receive' | 'item' | 'supplier';
export const PURCHASE_VIEWS: PurchaseView[] = ['receive', 'item', 'supplier'];
