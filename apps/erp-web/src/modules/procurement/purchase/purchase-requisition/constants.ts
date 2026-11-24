import { featurePath } from '@/common/constants/routes';
import { pathsOf, DOCUMENT_LOOKUPS } from '@/common/documents/documents';
import { PROCUREMENT_MODULE } from '../../procurement';

export const REQUISITION_FEATURE = 'procurement.purchase-requisition';
export const REQUISITION_PATH = featurePath(PROCUREMENT_MODULE, 'purchase-requisition');

export const requisitionPaths = pathsOf(REQUISITION_PATH);

export const REQUISITION_LOOKUPS = {
  ...DOCUMENT_LOOKUPS,
  requestType: 'requisition-type',
  purchaseMethod: 'purchase-method',
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

export const LINE_REMARKS_MAX = 300;
