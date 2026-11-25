import {
  approveSalesReturn,
  cancelSalesReturn,
  deleteSalesReturn,
  getGetReturnDeliveryOptionsQueryKey,
  getGetSalesReturnApprovalQueryKey,
  getSalesReturnApproval,
  rejectSalesReturn,
  useGetReturnDeliveryOptions,
  type CustomerOption,
  type ReturnDeliveryOption,
  type SalesReturnDetail,
  type SaveSalesReturnLine,
  type SaveSalesReturnRequest,
} from '@erp/api-client';
import type { FieldBehavior, LookupItem, LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { trimmed, useUnitKey } from '@/common/master-data';
import { usePayFrom } from '@/common/pay-from';
import { discountOf, salesTotals } from '../../pricing';
import {
  emptyLine,
  keepVisible,
  lineRequest,
  useSalesLookups,
  type SalesLineValue,
} from '../../sales-document';
import {
  FROM_DELIVERY,
  RETURN_LOOKUP_LISTS,
  RETURN_LOOKUPS,
  RETURN_SOURCES,
  WITHOUT_REFERENCE,
} from './constants';

export interface ReturnLine
  extends
    SalesLineValue,
    Partial<
      Pick<
        SaveSalesReturnLine,
        'deliveryLineId' | 'costPrice' | 'isDamaged' | 'batch' | 'itemSerial' | 'remarks'
      >
    > {
  remaining?: number | null;
}

export interface ReturnSource {
  isVatInclusive?: boolean;
  due?: number;
  reductionRate?: number;
}

export type ReturnValues = Omit<SaveSalesReturnRequest, 'lines'> &
  ReturnSource & { lines: ReturnLine[] };

export function useReturnLookups(enabled = true) {
  const { t } = useTranslation();
  const lookups = useSalesLookups(RETURN_LOOKUP_LISTS, enabled);
  const payFrom = usePayFrom(lookups[RETURN_LOOKUPS.paymentMethod], enabled);
  const deliveries = useGetReturnDeliveryOptions({
    query: { queryKey: useUnitKey(getGetReturnDeliveryOptionsQueryKey()), enabled },
  });
  const sources: LookupItem[] = RETURN_SOURCES.map((id) => ({
    id,
    name: t(`salesReturn.source.${id}`),
    isActive: true,
  }));
  const all: LookupSources = {
    ...lookups,
    ...payFrom.lookups,
    [RETURN_LOOKUPS.source]: sources,
    [RETURN_LOOKUPS.delivery]: deliveries.data,
  };
  return { lookups: all, payFrom, deliveries: deliveries.data };
}

const lockedByDelivery: FieldBehavior = {
  watch: ['source'],
  state: (values) => ({ disabled: values.source === FROM_DELIVERY }),
};

export const returnBehaviors = (
  payFrom: Record<string, FieldBehavior>,
): Record<string, FieldBehavior> => ({
  ...Object.fromEntries(Object.entries(payFrom).map(([key, b]) => [key, keepVisible(b)])),
  deliveryId: {
    watch: ['source'],
    state: (values) => ({ required: values.source === FROM_DELIVERY }),
  },
  customerId: lockedByDelivery,
  isVatInclusive: lockedByDelivery,
});

export function newReturn(): Partial<ReturnValues> {
  return {
    source: FROM_DELIVERY,
    returnDate: formatDate(new Date(), API_DATE_FORMAT),
    lines: [],
  };
}

export const ofSource = (source: string): Partial<ReturnValues> => ({
  deliveryId: null,
  isVatInclusive: false,
  due: 0,
  reductionRate: 0,
  lines: source === WITHOUT_REFERENCE ? [emptyLine()] : [],
});

export const fromReturnCustomer = (customer: CustomerOption): Partial<ReturnValues> => ({
  phone: customer.phone,
  pickupAddress: customer.address,
  ...(customer.salesRepresentativeId && { salesForceId: customer.salesRepresentativeId }),
});

export function fromDelivery(delivery: ReturnDeliveryOption): Partial<ReturnValues> {
  return {
    source: FROM_DELIVERY,
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
  const free = detail.source === WITHOUT_REFERENCE;
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
      ...(free
        ? { ...discountOf(line), costPrice: line.costPrice }
        : { discount: line.discountAmount / line.quantity, discountType: 'unit' as const }),
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
  const free = v.source === WITHOUT_REFERENCE;
  return {
    code: trimmed(v.code),
    source: v.source,
    returnDate: v.returnDate,
    deliveryId: free ? null : (v.deliveryId ?? null),
    customerId: v.customerId,
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
    isVatInclusive: !!v.isVatInclusive,
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
      ...lineRequest(line),
      deliveryLineId: free ? null : (line.deliveryLineId ?? null),
      costPrice: free ? (line.costPrice ?? null) : null,
      isDamaged: !!line.isDamaged,
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
