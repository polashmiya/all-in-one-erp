import { featurePath } from '@/common/constants/routes';
import { pathsOf } from '@/common/documents/documents';
import { SALES_MODULE } from '../../sales';
import { SALES_LOOKUPS } from '../../sales-document';

export const RETURN_FEATURE = 'sales.sales-return';
export const RETURN_PATH = featurePath(SALES_MODULE, 'sales-return');

export const DELIVERY_PARAM = 'delivery';

const paths = pathsOf(RETURN_PATH);

export const returnPaths = {
  ...paths,
  fromDelivery: (id: number) => `${paths.create}?${DELIVERY_PARAM}=${id}`,
};

export const RETURN_LOOKUPS = {
  ...SALES_LOOKUPS,
  delivery: 'sales-delivery',
  reason: 'sales-return-reason',
} as const;

export const RETURN_LOOKUP_LISTS = [
  RETURN_LOOKUPS.paymentMethod,
  RETURN_LOOKUPS.reason,
  'sales-return-type',
  'delivery-method',
];

export const BATCH_MAX = 500;
export const SERIAL_MAX = 2000;
export const REMARKS_MAX = 500;
