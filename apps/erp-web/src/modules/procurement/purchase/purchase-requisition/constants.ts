import type { StatusTone } from '@erp/ui';
import { featurePath } from '@/common/constants/routes';
import { PROCUREMENT_MODULE } from '../../procurement';

export const REQUISITION_FEATURE = 'procurement.purchase-requisition';
export const REQUISITION_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-requisition');

export const requisitionPaths = {
  list: REQUISITION_PATH,
  create: `${REQUISITION_PATH}/new`,
  view: (id: number) => `${REQUISITION_PATH}/${id}`,
  edit: (id: number) => `${REQUISITION_PATH}/${id}/edit`,
};

export const REQUISITION_LOOKUPS = {
  warehouse: 'warehouse',
  employee: 'employee',
  requestType: 'requisition-type',
  purchaseMethod: 'purchase-method',
  itemType: 'item-type',
  department: 'employee-department',
  section: 'department-section',
} as const;

export const REQUISITION_LOOKUP_LISTS = [
  REQUISITION_LOOKUPS.requestType,
  REQUISITION_LOOKUPS.purchaseMethod,
  REQUISITION_LOOKUPS.itemType,
  REQUISITION_LOOKUPS.department,
  REQUISITION_LOOKUPS.section,
];

export type RequisitionStatus = 'pending' | 'approved' | 'rejected' | 'closed';
export type RequisitionView = 'all' | RequisitionStatus;

export const REQUISITION_VIEWS: RequisitionView[] = [
  'all',
  'pending',
  'approved',
  'rejected',
  'closed',
];

export const STATUS_TONES: Record<string, StatusTone> = {
  pending: 'warning',
  approved: 'success',
  rejected: 'danger',
  closed: 'neutral',
};

export const QUANTITY_DISPLAY_DECIMALS = 2;
export const LINE_REMARKS_MAX = 300;
export const REVIEW_COMMENT_MAX = 500;
