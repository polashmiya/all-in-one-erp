import {
  approveSalesDelivery,
  cancelSalesDelivery,
  deleteSalesDelivery,
  getGetDeliveryOrderOptionsQueryKey,
  getGetSalesDeliveryApprovalQueryKey,
  getSalesDeliveryApproval,
  rejectSalesDelivery,
  useGetDeliveryOrderOptions,
  type CustomerOption,
  type DeliveryOrderOption,
  type SalesDeliveryDetail,
  type SaveSalesDeliveryLine,
  type SaveSalesDeliveryRequest,
} from '@erp/api-client';
import type { FieldBehavior, LookupItem, LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { trimmed, useUnitKey } from '@/common/master-data';
import { usePayFrom } from '@/common/pay-from';
import {
  discountBehaviors,
  documentRequest,
  emptyLine,
  fromCustomer,
  keepVisible,
  lineRequest,
  lineValue,
  useSalesLookups,
  type SalesLineValue,
} from '../../sales-document';
import {
  DELIVERY_LOOKUP_LISTS,
  DELIVERY_LOOKUPS,
  DELIVERY_TYPES,
  DIRECT,
  FROM_ORDER,
} from './constants';

export interface DeliveryLine
  extends
    SalesLineValue,
    Partial<
      Pick<
        SaveSalesDeliveryLine,
        | 'orderLineId'
        | 'batch'
        | 'itemSerial'
        | 'packQuantity'
        | 'warrantyDescription'
        | 'warrantyExpiredDate'
      >
    > {
  remaining?: number | null;
}

export type DeliveryValues = Omit<SaveSalesDeliveryRequest, 'lines'> & { lines: DeliveryLine[] };

export function useDeliveryLookups(enabled = true) {
  const { t } = useTranslation();
  const lookups = useSalesLookups(DELIVERY_LOOKUP_LISTS, enabled);
  const payFrom = usePayFrom(lookups[DELIVERY_LOOKUPS.paymentMethod], enabled);
  const orders = useGetDeliveryOrderOptions({
    query: { queryKey: useUnitKey(getGetDeliveryOrderOptionsQueryKey()), enabled },
  });
  const types: LookupItem[] = DELIVERY_TYPES.map((id) => ({
    id,
    name: t(`salesDelivery.type.${id}`),
    isActive: true,
  }));
  const all: LookupSources = {
    ...lookups,
    ...payFrom.lookups,
    [DELIVERY_LOOKUPS.type]: types,
    [DELIVERY_LOOKUPS.order]: orders.data,
  };
  return { lookups: all, payFrom, orders: orders.data };
}

const lockedByOrder: FieldBehavior = {
  watch: ['type'],
  state: (values) => ({ disabled: values.type === FROM_ORDER }),
};

const discountLock = (behavior: FieldBehavior | undefined): FieldBehavior => ({
  watch: [...(behavior?.watch ?? []), 'type'],
  state: (values) =>
    values.type === FROM_ORDER ? { disabled: true } : (behavior?.state(values) ?? {}),
});

export const deliveryBehaviors = (
  payFrom: Record<string, FieldBehavior>,
): Record<string, FieldBehavior> => ({
  ...Object.fromEntries(Object.entries(payFrom).map(([key, b]) => [key, keepVisible(b)])),
  orderId: {
    watch: ['type'],
    state: (values) => ({ required: values.type === FROM_ORDER }),
  },
  orderDate: lockedByOrder,
  isVatInclusive: lockedByOrder,
  isGrossDiscount: lockedByOrder,
  headerDiscountPercentage: discountLock(discountBehaviors.headerDiscountPercentage),
  headerDiscount: discountLock(discountBehaviors.headerDiscount),
  advanceBalanceAdjust: {
    watch: ['type'],
    state: (values) => ({ disabled: values.type !== FROM_ORDER }),
  },
});

export function newDelivery(): Partial<DeliveryValues> {
  const today = formatDate(new Date(), API_DATE_FORMAT);
  return { type: FROM_ORDER, orderDate: today, deliveryDate: today, lines: [] };
}

export const ofType = (type: string): Partial<DeliveryValues> => ({
  orderId: null,
  isGrossDiscount: false,
  advanceBalanceAdjust: null,
  lines: type === DIRECT ? [emptyLine()] : [],
});

export const fromDeliveryCustomer = (customer: CustomerOption): Partial<DeliveryValues> => ({
  ...fromCustomer(customer),
  phone: customer.phone,
});

export function fromOrder(order: DeliveryOrderOption): Partial<DeliveryValues> {
  return {
    type: FROM_ORDER,
    orderId: order.id,
    orderDate: order.orderDate,
    customerId: order.customerId,
    phone: order.phone,
    officeId: order.officeId,
    warehouseId: order.warehouseId,
    salesForceId: order.salesForceId,
    priceTypeId: order.priceTypeId,
    projectName: order.projectName,
    referredByPartnerId: order.referredByPartnerId,
    customerPo: order.customerPo,
    customerPoDate: order.customerPoDate,
    paymentTypeId: order.paymentTypeId,
    paymentTermDays: order.paymentTermDays,
    shippingContactPerson: order.shippingContactPerson,
    shippingAddressId: order.shippingAddressId,
    shippingAddressName: order.shippingAddressName,
    isVatInclusive: order.isVatInclusive,
    isGrossDiscount: false,
    specialDiscountAmount: order.specialDiscountAmount,
    othersCost: order.othersCost,
    otherChargeAccountId: order.otherChargeAccountId,
    extraCharge: order.extraCharge,
    extraChargeAccountId: order.extraChargeAccountId,
    deductionLabel: order.deductionLabel,
    deductionValue: order.deductionValue,
    freeTypeId: order.freeTypeId,
    currencyId: order.currencyId,
    exchangeRate: order.exchangeRate,
    isAdvanceAdjust: order.isAdvanceAdjust,
    advanceBalanceAdjust: order.isAdvanceAdjust ? order.advanceAvailable : null,
    lines: order.lines.map((line) => ({
      orderLineId: line.orderLineId,
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
      warrantyInMonth: line.warrantyInMonth,
      shelfLife: line.shelfLife,
      isOfferItem: line.isOfferItem,
    })),
  };
}

export function fromDetail(detail: SalesDeliveryDetail): DeliveryValues {
  const ordered = detail.type === FROM_ORDER;
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      ...lineValue(line),
      ...(ordered && {
        discount: line.discountAmount / line.quantity,
        discountType: 'unit' as const,
      }),
      orderLineId: line.orderLineId,
      remaining: line.remaining,
      batch: line.batch,
      itemSerial: line.itemSerial,
      packQuantity: line.packQuantity,
      warrantyDescription: line.warrantyDescription,
      warrantyExpiredDate: line.warrantyExpiredDate,
    })),
  };
}

export function toRequest(v: DeliveryValues): SaveSalesDeliveryRequest {
  const ordered = v.type === FROM_ORDER;
  return {
    ...documentRequest(v),
    type: v.type,
    orderId: ordered ? (v.orderId ?? null) : null,
    phone: trimmed(v.phone),
    challanNo: trimmed(v.challanNo),
    vatChallanNo: trimmed(v.vatChallanNo),
    billNo: trimmed(v.billNo),
    refInvoice: trimmed(v.refInvoice),
    anonymousAddress: trimmed(v.anonymousAddress),
    narration: trimmed(v.narration),
    deliveryMethodId: v.deliveryMethodId ?? null,
    courierOrEmployeeId: v.courierOrEmployeeId ?? null,
    submissionDate: v.submissionDate ?? null,
    alternativeDeliveryDate: v.alternativeDeliveryDate ?? null,
    advanceBalanceAdjust: ordered ? (v.advanceBalanceAdjust ?? null) : null,
    receiveAmount: v.receiveAmount ?? null,
    interestRate: v.interestRate ?? null,
    totalNoOfInstallment: v.totalNoOfInstallment ?? null,
    installmentStartDate: v.installmentStartDate ?? null,
    installmentTypeId: v.installmentTypeId ?? null,
    commissionPercentage: v.commissionPercentage ?? null,
    isExportSales: !!v.isExportSales,
    lines: v.lines.map((line) => ({
      ...lineRequest(line),
      orderLineId: ordered ? (line.orderLineId ?? null) : null,
      batch: trimmed(line.batch),
      itemSerial: trimmed(line.itemSerial),
      packQuantity: trimmed(line.packQuantity),
      warrantyDescription: trimmed(line.warrantyDescription),
      warrantyExpiredDate: line.warrantyExpiredDate ?? null,
    })),
  };
}

export const deliveryCalls: ApprovalCalls = {
  approve: (id, data) => approveSalesDelivery(id, data),
  reject: (id, data) => rejectSalesDelivery(id, data),
  cancel: (id, data) => cancelSalesDelivery(id, data),
  remove: (id) => deleteSalesDelivery(id),
  progress: (id) => getSalesDeliveryApproval(id),
  progressKey: (id) => getGetSalesDeliveryApprovalQueryKey(id),
};
