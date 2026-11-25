import { featurePath } from '@/common/constants/routes';
import { SALES_MODULE } from '../../sales';

export const COLLECTION_REPORT_FEATURE = 'sales.collection-report';
export const COLLECTION_REPORT_PATH = featurePath(SALES_MODULE, 'collection-report');

export const COLLECTION_REPORT_VIEWS = [
  'collection',
  'customer',
  'method',
  'collector',
  'date',
] as const;
export type CollectionView = (typeof COLLECTION_REPORT_VIEWS)[number];
