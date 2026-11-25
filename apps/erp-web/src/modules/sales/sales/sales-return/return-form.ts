import {
  approveSalesReturn,
  cancelSalesReturn,
  deleteSalesReturn,
  getGetReturnDeliveryOptionsQueryKey,
  getGetSalesReturnApprovalQueryKey,
  getSalesReturnApproval,
  rejectSalesReturn,
  useGetReturnDeliveryOptions,
  type ReturnDeliveryOption,
  type SalesReturnDetail,
  type SaveSalesReturnLine,
  type SaveSalesReturnRequest,
} from '@erp/api-client';
import type { FieldBehavior, LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { trimmed, useUnitKey } from '@/common/master-data';
import { usePayFrom } from '@/common/pay-from';
import { salesTotals } from '../../pricing';
import { keepVisible, useSalesLookups, type SalesLineValue } from '../../sales-document';
import { RETURN_LOOKUP_LISTS, RETURN_LOOKUPS } from './constants';

export interface ReturnLine
  extends
    SalesLineValue,
    Partial<
      Pick<SaveSalesReturnLine, 'deliveryLineId' | 'isDamaged' | 'batch' | 'itemSerial' | 'remarks'>
    > {
  remaining?: number;
}

export interface ReturnSource {
  customerId?: number;
  isVatInclusive?: boolean;
  due?: number;
  reductionRate?: number;
}

export type ReturnValues = Omit<SaveSalesReturnRequest, 'lines'> &
  ReturnSource & { lines: ReturnLine[] };

export function useReturnLookups(enabled = true) {
  const lookups = useSalesLookups(RETURN_LOOKUP_LISTS, enabled);
  const payFrom = usePayFrom(lookups[RETURN_LOOKUPS.paymentMethod], enabled);
  const deliveries = useGetReturnDeliveryOptions({
    query: { queryKey: useUnitKey(getGetReturnDeliveryOptionsQueryKey()), enabled },
  });
  const all: LookupSources = {
    ...lookups,
    ...payFrom.lookups,
    [RETURN_LOOKUPS.delivery]: deliveries.data,
  };
  return { lookups: all, payFrom, deliveries: deliveries.data };
}

export const returnBehaviors = (
  payFrom: Record<string, FieldBehavior>,
): Record<string, FieldBehavior> => ({
  ...Object.fromEntries(Object.entries(payFrom).map(([key, b]) => [key, keepVisible(b)])),
  customerId: { watch: [], state: () => ({ disabled: true }) },
});

export function newReturn(): Partial<ReturnValues> {
  return { returnDate: formatDate(new Date(), API_DATE_FORMAT), lines: [] };
}

export function fromDelivery(delivery: ReturnDeliveryOption): Partial<ReturnValues> {
  return {
    deliveryId: delivery.id,
    customerId: delivery.customerId,
    phone: delivery.phone,
    officeId: delivery.officeId,
    warehouseId: delivery.warehouseId,
    salesForceId: delivery.salesForceId,
    challanNo: delivery.challanNo,
    pickupAddress: delivery.pickupAddress,
    isVatInclusive: delivery.isVatInclusive,
    due: delivery.due,
    reductionRate: delivery.reductionRate,
    lines: delivery.lines.map((line) => ({
      deliveryLineId: line.deliveryLineId,
      itemId: line.itemId,
      itemCode: line.itemCode,
      itemName: line.itemName,
      itemDescription: line.itemDescription,
      uomName: line.uomName,
      quantity: line.remaining,
      remaining: line.remaining,
      price: line.price,
      discount: line.discountPerUnit,
      discountType: 'unit',
      sdPercentage: line.sdPercentage,
      vatPercentage: line.vatPercentage,
      aitPercentage: line.aitPercentage,
      isOfferItem: line.isOfferItem,
      batch: line.batch,
      itemSerial: line.itemSerial,
    })),
  };
}

export function fromDetail(detail: SalesReturnDetail): ReturnValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      deliveryLineId: line.deliveryLineId,
      itemId: line.itemId,
      itemCode: line.itemCode,
      itemName: line.itemName,
      itemDescription: line.itemDescription,
      uomName: line.uomName,
      quantity: line.quantity,
      remaining: line.remaining,
      price: line.price,
      discount: line.discountAmount / line.quantity,
      discountType: 'unit',
      sdPercentage: line.sdPercentage,
      vatPercentage: line.vatPercentage,
      aitPercentage: line.aitPercentage,
      isOfferItem: line.isOfferItem,
      isDamaged: line.isDamaged,
      batch: line.batch,
      itemSerial: line.itemSerial,
      remarks: line.remarks,
    })),
  };
}

export function returnTotals(
  lines: ReturnLine[],
  source: ReturnSource,
  amounts: { charge: number; deduction: number; refund: number },
) {
  const value = salesTotals(lines, { isVatInclusive: source.isVatInclusive, charges: 0 }).total;
  const special = Math.round(value * (source.reductionRate ?? 0) * 100) / 100;
  const reductions = special + amounts.deduction;
  const net = value + amounts.charge - reductions;
  const dueAdjusted = Math.min(Math.max(net, 0), Math.max(source.due ?? 0, 0));
  return { reductions, dueAdjusted, advance: Math.max(0, net - dueAdjusted - amounts.refund) };
}

export function toRequest(v: ReturnValues): SaveSalesReturnRequest {
  return {
    code: trimmed(v.code),
    returnDate: v.returnDate,
    deliveryId: v.deliveryId,
    warehouseId: v.warehouseId,
    officeId: v.officeId ?? null,
    salesForceId: v.salesForceId ?? null,
    phone: trimmed(v.phone),
    returnTypeId: v.returnTypeId ?? null,
    returnReasonId: v.returnReasonId ?? null,
    receivedById: v.receivedById ?? null,
    returnRequestNo: trimmed(v.returnRequestNo),
    returnRefNo: trimmed(v.returnRefNo),
    returnRefDate: v.returnRefDate ?? null,
    returnRefAmount: v.returnRefAmount ?? null,
    creditNoteNo: trimmed(v.creditNoteNo),
    creditNoteDate: v.creditNoteDate ?? null,
    challanNo: trimmed(v.challanNo),
    vatChallanNo: trimmed(v.vatChallanNo),
    deliveryMethodId: v.deliveryMethodId ?? null,
    pickupAddress: trimmed(v.pickupAddress),
    isExchange: !!v.isExchange,
    chargeRefundAmount: v.chargeRefundAmount ?? null,
    deductionLabel: trimmed(v.deductionLabel),
    deductionValue: v.deductionValue ?? null,
    refundAmount: v.refundAmount ?? null,
    paymentMethodId: v.paymentMethodId ?? null,
    bankAccountId: v.bankAccountId ?? null,
    instrumentNo: trimmed(v.instrumentNo),
    instrumentDate: v.instrumentDate ?? null,
    remarks: trimmed(v.remarks),
    narration: trimmed(v.narration),
    lines: v.lines.map((line) => ({
      deliveryLineId: line.deliveryLineId ?? 0,
      quantity: line.quantity ?? 0,
      isDamaged: !!line.isDamaged,
      itemDescription: trimmed(line.itemDescription),
      batch: trimmed(line.batch),
      itemSerial: trimmed(line.itemSerial),
      remarks: trimmed(line.remarks),
    })),
    customData: v.customData ?? null,
  };
}

export const returnCalls: ApprovalCalls = {
  approve: (id, data) => approveSalesReturn(id, data),
  reject: (id, data) => rejectSalesReturn(id, data),
  cancel: (id, data) => cancelSalesReturn(id, data),
  remove: (id) => deleteSalesReturn(id),
  progress: (id) => getSalesReturnApproval(id),
  progressKey: (id) => getGetSalesReturnApprovalQueryKey(id),
};
