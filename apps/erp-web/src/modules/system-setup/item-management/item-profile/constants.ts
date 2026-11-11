import { featurePath } from '@/common/constants/routes';

export const ITEM_PROFILE_FEATURE = 'system-setup.item-profile';

export const ITEM_PROFILE_PATH = featurePath('system-setup', 'item-profile');

type Id = number | string;

export const itemPaths = {
  list: ITEM_PROFILE_PATH,
  create: `${ITEM_PROFILE_PATH}/new`,
  duplicate: (id: Id) => `${ITEM_PROFILE_PATH}/new?copy=${encodeURIComponent(id)}`,
  view: (id: Id) => `${ITEM_PROFILE_PATH}/${encodeURIComponent(id)}`,
  edit: (id: Id) => `${ITEM_PROFILE_PATH}/${encodeURIComponent(id)}/edit`,
};

export const ITEM_ASIDE_SECTIONS = ['usage', 'classification', 'barcode'];

export const ITEM_LOOKUPS = {
  department: 'item-department',
  category: 'item-category',
  subCategory: 'item-sub-category',
  minorCategory: 'item-minor-category',
  uom: 'uom',
  itemType: 'item-type',
  supplyType: 'vat-supply-type',
} as const;
