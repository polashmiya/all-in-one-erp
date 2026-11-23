import {
  approvePurchaseOrder,
  approvePurchaseRequisition,
  rejectPurchaseOrder,
  rejectPurchaseRequisition,
  type ReviewRequest,
} from '@erp/api-client';
import { featurePath } from '@/common/constants/routes';
import { orderPaths } from '@/modules/procurement/purchase/purchase-order/constants';
import { requisitionPaths } from '@/modules/procurement/purchase/purchase-requisition/constants';

export const MY_APPROVALS_FEATURE = 'approval.my-approvals';
export const MY_APPROVALS_PATH = featurePath('approval', 'my-approvals');

export type InboxView = 'waiting' | 'approved' | 'rejected';
export const INBOX_VIEWS: InboxView[] = ['waiting', 'approved', 'rejected'];

interface ApprovalDocument {
  label: string;
  path: (id: number) => string;
  approve: (id: number, data: ReviewRequest) => Promise<unknown>;
  reject: (id: number, data: ReviewRequest) => Promise<unknown>;
}

export const APPROVAL_DOCUMENTS: Record<string, ApprovalDocument> = {
  'procurement.purchase-requisition': {
    label: 'myApprovals.document.requisition',
    path: requisitionPaths.view,
    approve: approvePurchaseRequisition,
    reject: rejectPurchaseRequisition,
  },
  'procurement.purchase-order': {
    label: 'myApprovals.document.order',
    path: orderPaths.view,
    approve: approvePurchaseOrder,
    reject: rejectPurchaseOrder,
  },
};
