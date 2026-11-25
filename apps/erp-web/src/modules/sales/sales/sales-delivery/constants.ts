import { featurePath } from '@/common/constants/routes';
import { pathsOf } from '@/common/documents/documents';
import { SALES_MODULE } from '../../sales';
import { SALES_LOOKUP_LISTS, SALES_LOOKUPS } from '../../sales-document';

export const DELIVERY_FEATURE = 'sales.sales-delivery';
export const DELIVERY_PATH = featurePath(SALES_MODULE, 'sales-delivery');

export const ORDER_PARAM = 'order';

const paths = pathsOf(DELIVERY_PATH);

export const deliveryPaths = {
  ...paths,
  fromOrder: (id: number) => `${paths.create}?${ORDER_PARAM}=${id}`,
};

export const DELIVERY_LOOKUPS = {
  ...SALES_LOOKUPS,
  type: 'sales-delivery-type',
  order: 'sales-order',
} as const;

export const DELIVERY_LOOKUP_LISTS = [...SALES_LOOKUP_LISTS, 'delivery-method', 'installment-type'];

export const FROM_ORDER = 'order';
export const DIRECT = 'direct';
export const DELIVERY_TYPES = [FROM_ORDER, DIRECT];

export const BATCH_MAX = 500;
export const SERIAL_MAX = 2000;
export const PACK_MAX = 50;
export const WARRANTY_TEXT_MAX = 500;
export const GRN_MAX = 150;
export const CONFIRM_REMARKS_MAX = 500;
