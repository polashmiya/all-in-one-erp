import { featurePath } from '@/common/constants/routes';
import { pathsOf } from '@/common/documents/documents';
import { SALES_MODULE } from '../../sales';
import { SALES_LOOKUP_LISTS, SALES_LOOKUPS } from '../../sales-document';

export const ORDER_FEATURE = 'sales.sales-order';
export const ORDER_PATH = featurePath(SALES_MODULE, 'sales-order');

export const QUOTATION_PARAM = 'quotation';

const paths = pathsOf(ORDER_PATH);

export const orderPaths = {
  ...paths,
  fromQuotation: (id: number) => `${paths.create}?${QUOTATION_PARAM}=${id}`,
};

export const ORDER_LOOKUPS = { ...SALES_LOOKUPS, quotation: 'sales-quotation' } as const;

export const ORDER_LOOKUP_LISTS = [...SALES_LOOKUP_LISTS, 'sales-term', 'lc-type', 'payment-terms'];
