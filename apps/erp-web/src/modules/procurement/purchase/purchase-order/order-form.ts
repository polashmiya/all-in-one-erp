import {
  approvePurchaseOrder,
  closePurchaseOrder,
  deletePurchaseOrder,
  getGetOrderRequisitionOptionsQueryKey,
  getGetSupplierOptionsQueryKey,
  rejectPurchaseOrder,
  getGetPurchaseOrderApprovalQueryKey,
  getPurchaseOrderApproval,
  useGetOrderRequisitionOptions,
  useGetSupplierOptions,
  type PurchaseOrderDetail,
  type SavePurchaseOrderLine,
  type SavePurchaseOrderRequest,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed, useUnitKey } from '@/common/master-data';
import type { ApprovalCalls } from '../../components/useApprovalActions';
import { useUnitWarehouses } from '../../procurement';
import { ORDER_LOOKUP_LISTS, ORDER_LOOKUPS } from './constants';

export interface OrderLineValue extends SavePurchaseOrderLine {
  itemCode?: string;
  itemName?: string;
  uomName?: string;
}

export type OrderLine = Partial<OrderLineValue>;

export type OrderValues = Omit<SavePurchaseOrderRequest, 'lines'> & { lines: OrderLine[] };

export function useOrderLookups(enabled = true): LookupSources {
  const lists = useLookupLists(ORDER_LOOKUP_LISTS, enabled);
  const warehouses = useUnitWarehouses(enabled);
  const suppliers = useGetSupplierOptions({
    query: { queryKey: useUnitKey(getGetSupplierOptionsQueryKey()), enabled },
  });
  const requisitions = useGetOrderRequisitionOptions({
    query: { queryKey: useUnitKey(getGetOrderRequisitionOptionsQueryKey()), enabled },
  });

  return {
    ...lists,
    [ORDER_LOOKUPS.warehouse]: warehouses,
    [ORDER_LOOKUPS.supplier]: suppliers.data,
    [ORDER_LOOKUPS.requisition]: requisitions.data,
  };
}

export const emptyLine = (): OrderLine => ({ quantity: 1, price: null, discount: null });

export function newOrder(): Partial<OrderValues> {
  return {
    orderDate: formatDate(new Date(), API_DATE_FORMAT),
    isVatInclusive: false,
    lines: [emptyLine()],
  };
}

export function fromDetail(detail: PurchaseOrderDetail): OrderValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      itemId: line.itemId,
      itemCode: line.itemCode,
      itemName: line.itemName,
      uomName: line.uomName,
      quantity: line.quantity,
      price: line.price,
      discount: line.discount,
      vatPercentage: line.vatPercentage,
      specification: line.specification,
    })),
  };
}

export function toRequest(v: OrderValues): SavePurchaseOrderRequest {
  return {
    code: trimmed(v.code),
    orderDate: v.orderDate,
    supplierId: v.supplierId,
    warehouseId: v.warehouseId,
    requisitionId: v.requisitionId ?? null,
    referenceNo: trimmed(v.referenceNo),
    expectedReceiveDate: v.expectedReceiveDate ?? null,
    paymentTypeId: v.paymentTypeId ?? null,
    paymentDays: v.paymentDays ?? null,
    isVatInclusive: !!v.isVatInclusive,
    itemTypeId: v.itemTypeId ?? null,
    shippingAddress: trimmed(v.shippingAddress),
    headerDiscount: v.headerDiscount ?? null,
    deliveryCharge: v.deliveryCharge ?? null,
    laborCharge: v.laborCharge ?? null,
    otherCharge: v.otherCharge ?? null,
    remarks: trimmed(v.remarks),
    lines: v.lines.map((line) => ({
      itemId: line.itemId ?? 0,
      quantity: line.quantity ?? 0,
      price: line.price ?? null,
      discount: line.discount ?? null,
      vatPercentage: line.vatPercentage ?? null,
      specification: trimmed(line.specification),
    })),
    customData: v.customData ?? null,
  };
}

export const orderCalls: ApprovalCalls = {
  approve: (id, data) => approvePurchaseOrder(id, data),
  reject: (id, data) => rejectPurchaseOrder(id, data),
  close: (id) => closePurchaseOrder(id),
  remove: (id) => deletePurchaseOrder(id),
  progress: (id) => getPurchaseOrderApproval(id),
  progressKey: (id) => getGetPurchaseOrderApprovalQueryKey(id),
};
