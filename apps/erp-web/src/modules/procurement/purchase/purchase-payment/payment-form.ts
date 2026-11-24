import {
  approvePurchasePayment,
  cancelPurchasePayment,
  deletePurchasePayment,
  getGetPurchasePaymentApprovalQueryKey,
  getGetSupplierOptionsQueryKey,
  getPurchasePaymentApproval,
  rejectPurchasePayment,
  useGetSupplierOptions,
  type PayableBill,
  type PurchasePaymentDetail,
  type SavePurchasePaymentLine,
  type SavePurchasePaymentRequest,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed, useUnitKey } from '@/common/master-data';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { usePayFrom } from '@/common/pay-from';
import { PAYMENT_LOOKUP_LISTS, PAYMENT_LOOKUPS } from './constants';

export interface PaymentLine extends Partial<SavePurchasePaymentLine> {
  code?: string;
  receiveDate?: string;
  purchaseOrderName?: string | null;
  billAmount?: number;
  due?: number;
}

export type PaymentValues = Omit<SavePurchasePaymentRequest, 'lines'> & { lines: PaymentLine[] };

export function usePaymentLookups(enabled = true) {
  const lists = useLookupLists(PAYMENT_LOOKUP_LISTS, enabled);
  const suppliers = useGetSupplierOptions({
    query: { queryKey: useUnitKey(getGetSupplierOptionsQueryKey()), enabled },
  });

  const payFrom = usePayFrom(lists[PAYMENT_LOOKUPS.method], enabled);
  const lookups: LookupSources = {
    ...lists,
    ...payFrom.lookups,
    [PAYMENT_LOOKUPS.supplier]: suppliers.data,
  };
  return { lookups, suppliers: suppliers.data, payFrom };
}

export const settledOf = (line: PaymentLine | undefined) =>
  (line?.paymentAmount ?? 0) +
  (line?.vds ?? 0) +
  (line?.tds ?? 0) +
  (line?.discountAmount ?? 0) +
  (line?.advanceAdjusted ?? 0);

export const cents = (amount: number) => Math.round(amount * 100);

export function paymentTotals(lines: PaymentLine[]) {
  const sum = (pick: (line: PaymentLine) => number | null | undefined) =>
    lines.reduce((total, line) => total + (pick(line) ?? 0), 0);
  return {
    payment: sum((l) => l.paymentAmount),
    vds: sum((l) => l.vds),
    tds: sum((l) => l.tds),
    discount: sum((l) => l.discountAmount),
    advance: sum((l) => l.advanceAdjusted),
    settled: sum(settledOf),
  };
}

export function newPayment(): Partial<PaymentValues> {
  return { paymentDate: formatDate(new Date(), API_DATE_FORMAT), lines: [] };
}

export function fromBill(bill: PayableBill): PaymentLine {
  return {
    purchaseReceiveId: bill.id,
    code: bill.code,
    receiveDate: bill.receiveDate,
    purchaseOrderName: bill.purchaseOrderName,
    billAmount: bill.netAmount - bill.returnAmount,
    due: bill.due,
    paymentAmount: bill.due,
  };
}

export function fromDetail(detail: PurchasePaymentDetail): PaymentValues {
  return {
    ...detail,
    advanceAmount: detail.isAdvance ? detail.paymentAmount : null,
    lines: detail.lines.map((line) => ({
      purchaseReceiveId: line.purchaseReceiveId,
      code: line.purchaseReceiveCode,
      receiveDate: line.receiveDate,
      purchaseOrderName: line.purchaseOrderName,
      billAmount: line.billAmount,
      due: line.due,
      paymentAmount: line.paymentAmount,
      vds: line.vds || null,
      tds: line.tds || null,
      discountAmount: line.discountAmount || null,
      advanceAdjusted: line.advanceAdjusted || null,
    })),
  };
}

export function toRequest(v: PaymentValues, cash: boolean): SavePurchasePaymentRequest {
  return {
    code: trimmed(v.code),
    paymentDate: v.paymentDate,
    supplierId: v.supplierId,
    isAdvance: !!v.isAdvance,
    paymentMethodId: v.paymentMethodId ?? null,
    bankAccountId: v.bankAccountId ?? null,
    advanceAmount: v.isAdvance ? (v.advanceAmount ?? null) : null,
    payeeName: trimmed(v.payeeName),
    bankAccountNo: cash ? null : trimmed(v.bankAccountNo),
    instrumentNo: cash ? null : trimmed(v.instrumentNo),
    instrumentDate: cash ? null : (v.instrumentDate ?? null),
    remarks: trimmed(v.remarks),
    lines: (v.isAdvance ? [] : v.lines).map((line) => ({
      purchaseReceiveId: line.purchaseReceiveId ?? 0,
      paymentAmount: line.paymentAmount ?? 0,
      vds: line.vds ?? null,
      tds: line.tds ?? null,
      discountAmount: line.discountAmount ?? null,
      advanceAdjusted: line.advanceAdjusted ?? null,
    })),
    customData: v.customData ?? null,
  };
}

export const paymentCalls: ApprovalCalls = {
  approve: (id, data) => approvePurchasePayment(id, data),
  reject: (id, data) => rejectPurchasePayment(id, data),
  cancel: (id, data) => cancelPurchasePayment(id, data),
  remove: (id) => deletePurchasePayment(id),
  progress: (id) => getPurchasePaymentApproval(id),
  progressKey: (id) => getGetPurchasePaymentApprovalQueryKey(id),
};
