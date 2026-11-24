import { featurePath } from '@/common/constants/routes';
import { pathsOf, DOCUMENT_LOOKUPS } from '@/common/documents/documents';
import { PROCUREMENT_MODULE } from '../../procurement';

export const PAYMENT_FEATURE = 'procurement.purchase-payment';
export const PAYMENT_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-payment');

export const paymentPaths = pathsOf(PAYMENT_PATH);

export const PAYMENT_LOOKUPS = {
  ...DOCUMENT_LOOKUPS,
  method: 'payment-method',
} as const;

export const PAYMENT_LOOKUP_LISTS = [PAYMENT_LOOKUPS.method];

export const BANK_FIELDS = ['bankAccountNo', 'instrumentNo', 'instrumentDate'] as const;
