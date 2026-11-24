import {
  approvePurchaseReturn,
  deletePurchaseReturn,
  getGetPurchaseReturnApprovalQueryKey,
  getGetReturnReceiveOptionsQueryKey,
  getGetSupplierOptionsQueryKey,
  getPurchaseReturnApproval,
  rejectPurchaseReturn,
  useGetReturnReceiveOptions,
  useGetSupplierOptions,
  type PurchaseReturnDetail,
  type ReturnReceiveOption,
  type SavePurchaseReturnLine,
  type SavePurchaseReturnRequest,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed, useUnitKey } from '@/common/master-data';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { useUnitWarehouses } from '@/common/documents/documents';
import { RETURN_LOOKUP_LISTS, RETURN_LOOKUPS } from './constants';

export interface ReturnLineValue extends SavePurchaseReturnLine {
  itemCode?: string;
  itemName?: string;
  uomName?: string;
  remaining?: number;
}

export type ReturnLine = Partial<ReturnLineValue>;

export type ReturnValues = Omit<SavePurchaseReturnRequest, 'lines'> & {
  supplierId?: number;
  lines: ReturnLine[];
};

export function useReturnLookups(enabled = true) {
  const lists = useLookupLists(RETURN_LOOKUP_LISTS, enabled);
  const warehouses = useUnitWarehouses(enabled);
  const suppliers = useGetSupplierOptions({
    query: { queryKey: useUnitKey(getGetSupplierOptionsQueryKey()), enabled },
  });
  const receives = useGetReturnReceiveOptions({
    query: { queryKey: useUnitKey(getGetReturnReceiveOptionsQueryKey()), enabled },
  });

  const lookups: LookupSources = {
    ...lists,
    [RETURN_LOOKUPS.warehouse]: warehouses,
    [RETURN_LOOKUPS.supplier]: suppliers.data,
    [RETURN_LOOKUPS.receive]: receives.data,
  };
  return { lookups, receives: receives.data };
}

export const emptyLine = (): ReturnLine => ({ quantity: 1 });

export function newReturn(): Partial<ReturnValues> {
  return { returnDate: formatDate(new Date(), API_DATE_FORMAT), isVatInclusive: false, lines: [] };
}

export function fromReceive(receive: ReturnReceiveOption): Partial<ReturnValues> {
  return {
    supplierId: receive.supplierId,
    warehouseId: receive.warehouseId,
    isVatInclusive: receive.isVatInclusive,
    lines: receive.lines.map((line) => ({
      itemId: line.itemId,
      itemCode: line.itemCode,
      itemName: line.itemName,
      uomName: line.uomName,
      remaining: line.remaining,
      quantity: line.remaining,
      price: line.price,
      discount: Math.round(line.discount * line.remaining * 100) / 100 || null,
      vatPercentage: line.vatPercentage,
      batch: line.batch,
    })),
  };
}

export function fromDetail(detail: PurchaseReturnDetail): ReturnValues {
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

export function toRequest(v: ReturnValues): SavePurchaseReturnRequest {
  return {
    code: trimmed(v.code),
    returnDate: v.returnDate,
    purchaseReceiveId: v.purchaseReceiveId,
    warehouseId: v.warehouseId,
    reasonId: v.reasonId ?? null,
    isVatInclusive: !!v.isVatInclusive,
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

export const returnCalls: ApprovalCalls = {
  approve: (id, data) => approvePurchaseReturn(id, data),
  reject: (id, data) => rejectPurchaseReturn(id, data),
  remove: (id) => deletePurchaseReturn(id),
  progress: (id) => getPurchaseReturnApproval(id),
  progressKey: (id) => getGetPurchaseReturnApprovalQueryKey(id),
};
