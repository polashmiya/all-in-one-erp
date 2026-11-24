import { featurePath } from '@/common/constants/routes';
import { ACCOUNTING_MODULE } from '../../accounting';

export const POSTING_FEATURE = 'accounting.posting-accounts';
export const POSTING_PATH = featurePath(ACCOUNTING_MODULE, 'posting-accounts');

export const POSTING_SECTIONS = [
  { key: 'stock', keys: ['inventory', 'stock-gain', 'stock-loss', 'opening-balance'] },
  {
    key: 'purchase',
    keys: ['accounts-payable', 'vat-receivable', 'price-difference', 'purchase-discount'],
  },
  { key: 'payment', keys: ['cash', 'bank', 'mobile-banking', 'vds-payable', 'tds-payable'] },
];
