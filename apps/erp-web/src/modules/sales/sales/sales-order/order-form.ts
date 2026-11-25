import {
  approveSalesOrder,
  cancelSalesOrder,
  closeSalesOrder,
  deleteSalesOrder,
  getGetOrderQuotationOptionsQueryKey,
  getGetSalesOrderApprovalQueryKey,
  getSalesOrderApproval,
  rejectSalesOrder,
  useGetOrderQuotationOptions,
  type OrderQuotationOption,
  type SalesOrderDetail,
  type SaveSalesOrderLine,
  type SaveSalesOrderRequest,
} from '@erp/api-client';
import type { FieldBehavior, LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { trimmed, useUnitKey } from '@/common/master-data';
import { usePayFrom } from '@/common/pay-from';
import { discountOf } from '../../pricing';
import {
  discountBehaviors,
  documentRequest,
  emptyLine,
  keepVisible,
  lineRequest,
  lineValue,
  useSalesLookups,
  type SalesLineValue,
} from '../../sales-document';
import { ORDER_LOOKUP_LISTS, ORDER_LOOKUPS } from './constants';

export interface OrderLine
  extends
    SalesLineValue,
    Partial<Pick<SaveSalesOrderLine, 'demandQuantity' | 'fobRate' | 'freightRate'>> {}

export type OrderValues = Omit<SaveSalesOrderRequest, 'lines'> & { lines: OrderLine[] };

export function useOrderLookups(enabled = true) {
  const lookups = useSalesLookups(ORDER_LOOKUP_LISTS, enabled);
  const payFrom = usePayFrom(lookups[ORDER_LOOKUPS.paymentMethod], enabled);
  const quotations = useGetOrderQuotationOptions({
    query: { queryKey: useUnitKey(getGetOrderQuotationOptionsQueryKey()), enabled },
  });
  const all: LookupSources = {
    ...lookups,
    ...payFrom.lookups,
    [ORDER_LOOKUPS.quotation]: quotations.data,
  };
  return { lookups: all, payFrom, quotations: quotations.data };
}

const exportOnly: FieldBehavior = {
  watch: ['isForeign'],
  state: (values) => ({ disabled: !values.isForeign }),
};

export const orderBehaviors = (
  payFrom: Record<string, FieldBehavior>,
): Record<string, FieldBehavior> => ({
  ...discountBehaviors,
  ...Object.fromEntries(Object.entries(payFrom).map(([key, b]) => [key, keepVisible(b)])),
  lcTypeId: exportOnly,
  lcNo: exportOnly,
  paymentTermsId: exportOnly,
  isAllowPartialShipment: exportOnly,
  isAllowTransitShipment: exportOnly,
});

export function newOrder(): Partial<OrderValues> {
  const today = formatDate(new Date(), API_DATE_FORMAT);
  return { orderDate: today, deliveryDate: today, lines: [emptyLine()] };
}

export function fromQuotation(quotation: OrderQuotationOption): Partial<OrderValues> {
  return {
    salesQuotationId: quotation.id,
    customerId: quotation.customerId,
    officeId: quotation.officeId,
    salesForceId: quotation.salesForceId,
    shippingContactPerson: quotation.shippingContactPerson,
    shippingAddressId: quotation.shippingAddressId,
    shippingAddressName: quotation.shippingAddressName,
    isVatInclusive: quotation.isVatInclusive,
    isGrossDiscount: quotation.isGrossDiscount,
    headerDiscountPercentage: quotation.headerDiscountPercentage || null,
    headerDiscount: quotation.headerDiscount || null,
    othersCost: quotation.othersCost,
    extraCharge: quotation.extraCharge,
    lines: quotation.lines.map((line) => ({
      itemId: line.itemId,
      itemCode: line.itemCode ?? undefined,
      itemName: line.itemName,
      itemDescription: line.itemDescription,
      uomName: line.uomName,
      quantity: line.quantity,
      price: line.price,
      ...discountOf(line),
      sdPercentage: line.sdPercentage,
      vatPercentage: line.vatPercentage,
      aitPercentage: line.aitPercentage,
      warrantyInMonth: line.warrantyInMonth,
    })),
  };
}

export function fromDetail(detail: SalesOrderDetail): OrderValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      ...lineValue(line),
      demandQuantity: line.demandQuantity,
      fobRate: line.fobRate || null,
      freightRate: line.freightRate || null,
    })),
  };
}

export function toRequest(v: OrderValues): SaveSalesOrderRequest {
  return {
    ...documentRequest(v),
    salesQuotationId: v.salesQuotationId ?? null,
    advanceAmount: v.advanceAmount ?? null,
    dueLimit: v.dueLimit ?? null,
    customerPoAmount: v.customerPoAmount ?? null,
    customerPoRefAmount: v.customerPoRefAmount ?? null,
    salesTermId: v.salesTermId ?? null,
    dmsInvoice: trimmed(v.dmsInvoice),
    isForeign: !!v.isForeign,
    lcTypeId: v.lcTypeId ?? null,
    lcNo: trimmed(v.lcNo),
    paymentTermsId: v.paymentTermsId ?? null,
    isAllowPartialShipment: !!v.isAllowPartialShipment,
    isAllowTransitShipment: !!v.isAllowTransitShipment,
    review: trimmed(v.review),
    lines: v.lines.map((line) => ({
      ...lineRequest(line),
      demandQuantity: line.demandQuantity ?? null,
      fobRate: line.fobRate ?? null,
      freightRate: line.freightRate ?? null,
    })),
  };
}

export const orderCalls: ApprovalCalls = {
  approve: (id, data) => approveSalesOrder(id, data),
  reject: (id, data) => rejectSalesOrder(id, data),
  cancel: (id, data) => cancelSalesOrder(id, data),
  close: (id) => closeSalesOrder(id),
  remove: (id) => deleteSalesOrder(id),
  progress: (id) => getSalesOrderApproval(id),
  progressKey: (id) => getGetSalesOrderApprovalQueryKey(id),
};
