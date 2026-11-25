import { featurePath } from '@/common/constants/routes';
import { pathsOf } from '@/common/documents/documents';
import { SALES_MODULE } from '../../sales';
import { SALES_LOOKUPS } from '../../sales-document';

export const COLLECTION_FEATURE = 'sales.sales-collection';
export const COLLECTION_PATH = featurePath(SALES_MODULE, 'sales-collection');

export const CUSTOMER_PARAM = 'customer';

const paths = pathsOf(COLLECTION_PATH);

export const collectionPaths = {
  ...paths,
  fromCustomer: (id: number) => `${paths.create}?${CUSTOMER_PARAM}=${id}`,
};

export const COLLECTION_LOOKUPS = SALES_LOOKUPS;

export const COLLECTION_LOOKUP_LISTS = [
  COLLECTION_LOOKUPS.paymentMethod,
  'instrument-type',
  'currency',
];

export const BANK_FIELDS = [
  'instrumentTypeId',
  'instrumentNo',
  'instrumentDate',
  'depositDate',
  'customerBankName',
  'customerBankBranch',
  'transactionNo',
] as const;

export const AMOUNTS = [
  'collectionAmount',
  'vds',
  'ait',
  'discountAmount',
  'expenseAmount',
  'advanceAdjusted',
] as const;

export const REMARKS_MAX = 500;
