import { featurePath } from '@/common/constants/routes';
import { DOCUMENT_LOOKUPS, pathsOf } from '@/common/documents/documents';
import { SALES_MODULE } from '../../sales';

export const QUOTATION_FEATURE = 'sales.sales-quotation';
export const QUOTATION_PATH = featurePath(SALES_MODULE, 'sales-quotation');

export const quotationPaths = pathsOf(QUOTATION_PATH);

export const QUOTATION_LOOKUPS = {
  ...DOCUMENT_LOOKUPS,
  quotationType: 'quotation-type',
  shippingAddress: 'shipping-address',
} as const;

export const QUOTATION_LOOKUP_LISTS = [
  QUOTATION_LOOKUPS.quotationType,
  QUOTATION_LOOKUPS.shippingAddress,
];

export const FORM_COLUMNS = 6;
export const FIRST_VERSION = 'v1';
export const VALIDITY_DAYS = 30;
export const DESCRIPTION_MAX = 2000;
export const DELIVERY_MAX = 300;
export const WARRANTY_MAX = 1200;
export const SD_MAX = 1000;
