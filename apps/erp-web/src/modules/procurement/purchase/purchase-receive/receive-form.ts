import {
  approvePurchaseReceive,
  deletePurchaseReceive,
  getGetPurchaseReceiveApprovalQueryKey,
  getGetReceiveOrderOptionsQueryKey,
  getGetSupplierOptionsQueryKey,
  getPurchaseReceiveApproval,
  rejectPurchaseReceive,
  useGetReceiveOrderOptions,
  useGetSupplierOptions,
  type PurchaseReceiveDetail,
  type ReceiveOrderOption,
  type SavePurchaseReceiveLine,
  type SavePurchaseReceiveRequest,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed, useUnitKey } from '@/common/master-data';
import type { ApprovalCalls } from '../../components/useApprovalActions';
import { useUnitWarehouses } from '../../procurement';
import { RECEIVE_LOOKUP_LISTS, RECEIVE_LOOKUPS } from './constants';

export interface ReceiveLineValue extends SavePurchaseReceiveLine {
  itemCode?: string;
  itemName?: string;
  uomName?: string;
  remaining?: number;
}

export type ReceiveLine = Partial<ReceiveLineValue>;

export type ReceiveValues = Omit<SavePurchaseReceiveRequest, 'lines'> & {
  supplierId?: number;
  lines: ReceiveLine[];
};

export function useReceiveLookups(enabled = true) {
  const lists = useLookupLists(RECEIVE_LOOKUP_LISTS, enabled);
  const warehouses = useUnitWarehouses(enabled);
  const suppliers = useGetSupplierOptions({
    query: { queryKey: useUnitKey(getGetSupplierOptionsQueryKey()), enabled },
  });
  const orders = useGetReceiveOrderOptions({
    query: { queryKey: useUnitKey(getGetReceiveOrderOptionsQueryKey()), enabled },
  });

  const lookups: LookupSources = {
    ...lists,
    [RECEIVE_LOOKUPS.warehouse]: warehouses,
    [RECEIVE_LOOKUPS.supplier]: suppliers.data,
    [RECEIVE_LOOKUPS.order]: orders.data,
  };
  return { lookups, orders: orders.data };
}

export const emptyLine = (): ReceiveLine => ({ quantity: 1 });

export function newReceive(): Partial<ReceiveValues> {
  return {
    receiveDate: formatDate(new Date(), API_DATE_FORMAT),
    isVatInclusive: false,
    lines: [],
  };
}

export function fromOrder(
  order: ReceiveOrderOption,
): Pick<
  ReceiveValues,
  'supplierId' | 'warehouseId' | 'isVatInclusive' | 'paymentTypeId' | 'paymentDays' | 'lines'
> {
  return {
    supplierId: order.supplierId,
    warehouseId: order.warehouseId,
    isVatInclusive: order.isVatInclusive,
    paymentTypeId: order.paymentTypeId,
    paymentDays: order.paymentDays,
    lines: order.lines.map((line) => ({
      itemId: line.itemId,
      itemCode: line.itemCode,
      itemName: line.itemName,
      uomName: line.uomName,
      remaining: line.remaining,
      quantity: line.remaining,
      price: line.price,
      discount: Math.round(line.discount * line.remaining * 100) / 100 || null,
      vatPercentage: line.vatPercentage,
    })),
  };
}

export function fromDetail(detail: PurchaseReceiveDetail): ReceiveValues {
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
      batch: line.batch,
    })),
  };
}

export function toRequest(v: ReceiveValues): SavePurchaseReceiveRequest {
  return {
    code: trimmed(v.code),
    receiveDate: v.receiveDate,
    purchaseOrderId: v.purchaseOrderId,
    warehouseId: v.warehouseId,
    challanNo: trimmed(v.challanNo),
    challanDate: v.challanDate ?? null,
    vatChallanNo: trimmed(v.vatChallanNo),
    paymentTypeId: v.paymentTypeId ?? null,
    paymentDays: v.paymentDays ?? null,
    isVatInclusive: !!v.isVatInclusive,
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
      batch: trimmed(line.batch),
    })),
    customData: v.customData ?? null,
  };
}

export const receiveCalls: ApprovalCalls = {
  approve: (id, data) => approvePurchaseReceive(id, data),
  reject: (id, data) => rejectPurchaseReceive(id, data),
  remove: (id) => deletePurchaseReceive(id),
  progress: (id) => getPurchaseReceiveApproval(id),
  progressKey: (id) => getGetPurchaseReceiveApprovalQueryKey(id),
};
