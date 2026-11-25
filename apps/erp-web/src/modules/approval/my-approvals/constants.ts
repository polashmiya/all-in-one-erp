import {
  approveJournal,
  approvePurchaseOrder,
  approvePurchasePayment,
  approvePurchaseReceive,
  approvePurchaseRequisition,
  approveSalesQuotation,
  approvePurchaseReturn,
  approveStockAdjustment,
  approveStockTransfer,
  approveTaxDeposit,
  rejectJournal,
  rejectPurchaseOrder,
  rejectPurchasePayment,
  rejectPurchaseReceive,
  rejectPurchaseRequisition,
  rejectSalesQuotation,
  rejectPurchaseReturn,
  rejectStockAdjustment,
  rejectStockTransfer,
  rejectTaxDeposit,
  type ReviewRequest,
} from '@erp/api-client';
import { featurePath } from '@/common/constants/routes';
import { DOCUMENTS, type DocumentFeature, type DocumentLink } from '@/modules/document-registry';

export const MY_APPROVALS_FEATURE = 'approval.my-approvals';
export const MY_APPROVALS_PATH = featurePath('approval', 'my-approvals');

export type InboxView = 'waiting' | 'approved' | 'rejected';
export const INBOX_VIEWS: InboxView[] = ['waiting', 'approved', 'rejected'];

type Review = (id: number, data: ReviewRequest) => Promise<unknown>;

interface ApprovalDocument extends DocumentLink {
  approve: Review;
  reject: Review;
}

const reviews: Record<DocumentFeature, { approve: Review; reject: Review }> = {
  'sales.sales-quotation': { approve: approveSalesQuotation, reject: rejectSalesQuotation },
  'procurement.purchase-requisition': {
    approve: approvePurchaseRequisition,
    reject: rejectPurchaseRequisition,
  },
  'procurement.purchase-order': { approve: approvePurchaseOrder, reject: rejectPurchaseOrder },
  'procurement.purchase-receive': {
    approve: approvePurchaseReceive,
    reject: rejectPurchaseReceive,
  },
  'procurement.purchase-return': { approve: approvePurchaseReturn, reject: rejectPurchaseReturn },
  'procurement.purchase-payment': {
    approve: approvePurchasePayment,
    reject: rejectPurchasePayment,
  },
  'inventory.stock-adjustment': { approve: approveStockAdjustment, reject: rejectStockAdjustment },
  'inventory.stock-transfer': { approve: approveStockTransfer, reject: rejectStockTransfer },
  'accounting.journal': { approve: approveJournal, reject: rejectJournal },
  'accounting.tax-deposit': { approve: approveTaxDeposit, reject: rejectTaxDeposit },
};

export const APPROVAL_DOCUMENTS: Record<string, ApprovalDocument> = Object.fromEntries(
  (Object.keys(DOCUMENTS) as DocumentFeature[]).map((feature) => [
    feature,
    { ...DOCUMENTS[feature], ...reviews[feature] },
  ]),
);
