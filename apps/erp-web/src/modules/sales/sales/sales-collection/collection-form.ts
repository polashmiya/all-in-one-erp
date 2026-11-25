import {
  approveSalesCollection,
  cancelSalesCollection,
  deleteSalesCollection,
  getGetSalesCollectionApprovalQueryKey,
  getSalesCollectionApproval,
  rejectSalesCollection,
  type CollectableBill,
  type SalesCollectionDetail,
  type SaveSalesCollectionLine,
  type SaveSalesCollectionRequest,
} from '@erp/api-client';
import type { FieldBehavior, LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { trimmed } from '@/common/master-data';
import { usePayFrom, type PayFrom } from '@/common/pay-from';
import { keepVisible, useSalesLookups } from '../../sales-document';
import { AMOUNTS, BANK_FIELDS, COLLECTION_LOOKUP_LISTS, COLLECTION_LOOKUPS } from './constants';

export interface CollectionLine extends Partial<SaveSalesCollectionLine> {
  code?: string;
  deliveryDate?: string;
  dueDate?: string;
  orderCode?: string | null;
  billAmount?: number;
  due?: number;
}

export type CollectionValues = Omit<SaveSalesCollectionRequest, 'lines'> & {
  lines: CollectionLine[];
};

export function useCollectionLookups(enabled = true) {
  const lookups = useSalesLookups(COLLECTION_LOOKUP_LISTS, enabled);
  const payFrom = usePayFrom(lookups[COLLECTION_LOOKUPS.paymentMethod], enabled);
  const all: LookupSources = { ...lookups, ...payFrom.lookups };
  return { lookups: all, payFrom };
}

export function collectionBehaviors(payFrom: PayFrom): Record<string, FieldBehavior> {
  const bankField: FieldBehavior = {
    watch: ['paymentMethodId'],
    state: (values) => ({ disabled: payFrom.isCash(values.paymentMethodId) }),
  };
  return {
    ...Object.fromEntries(BANK_FIELDS.map((key) => [key, bankField])),
    ...Object.fromEntries(
      Object.entries(payFrom.behaviors).map(([key, b]) => [key, keepVisible(b)]),
    ),
    advanceAmount: {
      watch: ['isAdvance'],
      state: (values) => ({ disabled: !values.isAdvance, required: true }),
    },
  };
}

export const cents = (amount: number) => Math.round(amount * 100);

export const settledOf = (line: CollectionLine | undefined) =>
  AMOUNTS.reduce((total, key) => total + (line?.[key] ?? 0), 0);

export function collectionTotals(lines: CollectionLine[]) {
  const sum = (key: (typeof AMOUNTS)[number]) =>
    lines.reduce((total, line) => total + (line?.[key] ?? 0), 0);
  return {
    amounts: Object.fromEntries(AMOUNTS.map((key) => [key, sum(key)])) as Record<
      (typeof AMOUNTS)[number],
      number
    >,
    settled: lines.reduce((total, line) => total + settledOf(line), 0),
  };
}

export function newCollection(): Partial<CollectionValues> {
  return { collectionDate: formatDate(new Date(), API_DATE_FORMAT), lines: [] };
}

export function fromBill(bill: CollectableBill): CollectionLine {
  return {
    deliveryId: bill.id,
    code: bill.code,
    deliveryDate: bill.deliveryDate,
    dueDate: bill.dueDate,
    orderCode: bill.orderCode,
    billAmount: bill.billAmount,
    due: bill.due,
    collectionAmount: bill.due,
  };
}

export function fromDetail(detail: SalesCollectionDetail): CollectionValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      deliveryId: line.deliveryId,
      code: line.deliveryCode,
      deliveryDate: line.deliveryDate,
      orderCode: line.orderCode,
      billAmount: line.billAmount,
      due: line.due,
      collectionAmount: line.collectionAmount,
      vds: line.vds || null,
      ait: line.ait || null,
      discountAmount: line.discountAmount || null,
      expenseAmount: line.expenseAmount || null,
      advanceAdjusted: line.advanceAdjusted || null,
      remarks: line.remarks,
    })),
  };
}

export function toRequest(v: CollectionValues, cash: boolean): SaveSalesCollectionRequest {
  return {
    code: trimmed(v.code),
    collectionDate: v.collectionDate,
    customerId: v.customerId,
    isAdvance: !!v.isAdvance,
    advanceAmount: v.isAdvance ? (v.advanceAmount ?? null) : null,
    officeId: v.officeId ?? null,
    collectedById: v.collectedById ?? null,
    moneyReceiptNo: trimmed(v.moneyReceiptNo),
    referenceNo: trimmed(v.referenceNo),
    paymentMethodId: v.paymentMethodId ?? null,
    bankAccountId: v.bankAccountId ?? null,
    customerBankName: cash ? null : trimmed(v.customerBankName),
    customerBankBranch: cash ? null : trimmed(v.customerBankBranch),
    instrumentTypeId: cash ? null : (v.instrumentTypeId ?? null),
    instrumentNo: cash ? null : trimmed(v.instrumentNo),
    instrumentDate: cash ? null : (v.instrumentDate ?? null),
    depositDate: cash ? null : (v.depositDate ?? null),
    transactionNo: cash ? null : trimmed(v.transactionNo),
    vdsCertificateNo: trimmed(v.vdsCertificateNo),
    vdsCertificateDate: v.vdsCertificateDate ?? null,
    aitCertificateNo: trimmed(v.aitCertificateNo),
    aitCertificateDate: v.aitCertificateDate ?? null,
    currencyId: v.currencyId ?? null,
    exchangeRate: v.exchangeRate ?? null,
    remarks: trimmed(v.remarks),
    narration: trimmed(v.narration),
    lines: (v.isAdvance ? [] : v.lines).map((line) => ({
      deliveryId: line.deliveryId ?? 0,
      collectionAmount: line.collectionAmount ?? 0,
      vds: line.vds ?? null,
      ait: line.ait ?? null,
      discountAmount: line.discountAmount ?? null,
      expenseAmount: line.expenseAmount ?? null,
      advanceAdjusted: line.advanceAdjusted ?? null,
      remarks: trimmed(line.remarks),
    })),
    customData: v.customData ?? null,
  };
}

export const collectionCalls: ApprovalCalls = {
  approve: (id, data) => approveSalesCollection(id, data),
  reject: (id, data) => rejectSalesCollection(id, data),
  cancel: (id, data) => cancelSalesCollection(id, data),
  remove: (id) => deleteSalesCollection(id),
  progress: (id) => getSalesCollectionApproval(id),
  progressKey: (id) => getGetSalesCollectionApprovalQueryKey(id),
};
