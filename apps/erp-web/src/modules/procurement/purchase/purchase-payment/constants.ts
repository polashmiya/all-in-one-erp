import { featurePath } from '@/common/constants/routes';
import { pathsOf, PROCUREMENT_LOOKUPS, PROCUREMENT_MODULE } from '../../procurement';

export const PAYMENT_FEATURE = 'procurement.purchase-payment';
export const PAYMENT_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-payment');

export const paymentPaths = pathsOf(PAYMENT_PATH);

export const PAYMENT_LOOKUPS = {
  ...PROCUREMENT_LOOKUPS,
  method: 'payment-method',
} as const;

export const PAYMENT_LOOKUP_LISTS = [PAYMENT_LOOKUPS.method];

export const CASH_METHOD = 'CASH';

export const BANK_FIELDS = ['bankAccountNo', 'instrumentNo', 'instrumentDate'] as const;
