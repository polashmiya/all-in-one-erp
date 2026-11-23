import {
  approvePurchaseRequisition,
  closePurchaseRequisition,
  deletePurchaseRequisition,
  rejectPurchaseRequisition,
  getGetPurchaseRequisitionApprovalQueryKey,
  getPurchaseRequisitionApproval,
  getGetEmployeeOptionsQueryKey,
  useGetEmployeeOptions,
  type RequisitionDetail,
  type SaveRequisitionLine,
  type SaveRequisitionRequest,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed, useUnitKey } from '@/common/master-data';
import type { ApprovalCalls } from '../../components/useApprovalActions';
import { useUnitWarehouses } from '../../procurement';
import { REQUISITION_LOOKUP_LISTS, REQUISITION_LOOKUPS } from './constants';

export interface RequisitionLineValue extends SaveRequisitionLine {
  itemCode?: string;
  itemName?: string;
  uomName?: string;
}

export type RequisitionValues = Omit<SaveRequisitionRequest, 'lines'> & {
  lines: Partial<RequisitionLineValue>[];
};

export function useRequisitionLookups(enabled = true): LookupSources {
  const lists = useLookupLists(REQUISITION_LOOKUP_LISTS, enabled);
  const warehouses = useUnitWarehouses(enabled);
  const employees = useGetEmployeeOptions({
    query: { queryKey: useUnitKey(getGetEmployeeOptionsQueryKey()), enabled },
  });

  return {
    ...lists,
    [REQUISITION_LOOKUPS.warehouse]: warehouses,
    [REQUISITION_LOOKUPS.employee]: employees.data,
  };
}

export const emptyLine = (): Partial<RequisitionLineValue> => ({ quantity: 1, rate: null });

export function newRequisition(): Partial<RequisitionValues> {
  return { requestDate: formatDate(new Date(), API_DATE_FORMAT), lines: [emptyLine()] };
}

export function fromDetail(detail: RequisitionDetail): RequisitionValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      itemId: line.itemId,
      itemCode: line.itemCode,
      itemName: line.itemName,
      uomName: line.uomName,
      quantity: line.quantity,
      rate: line.rate,
      remarks: line.remarks,
    })),
  };
}

export function toRequest(v: RequisitionValues): SaveRequisitionRequest {
  return {
    code: trimmed(v.code),
    requestDate: v.requestDate,
    dueDate: v.dueDate ?? null,
    warehouseId: v.warehouseId,
    requestTypeId: v.requestTypeId ?? null,
    purchaseMethodId: v.purchaseMethodId ?? null,
    itemTypeId: v.itemTypeId ?? null,
    departmentId: v.departmentId ?? null,
    sectionId: v.sectionId ?? null,
    employeeId: v.employeeId ?? null,
    buyerId: v.buyerId ?? null,
    projectName: trimmed(v.projectName),
    remarks: trimmed(v.remarks),
    lines: v.lines.map((line) => ({
      itemId: line.itemId ?? 0,
      quantity: line.quantity ?? 0,
      rate: line.rate ?? null,
      remarks: trimmed(line.remarks),
    })),
    customData: v.customData ?? null,
  };
}

export const lineAmount = (line?: Partial<RequisitionLineValue>) =>
  (line?.quantity ?? 0) * (line?.rate ?? 0);

export const requisitionCalls: ApprovalCalls = {
  approve: (id, data) => approvePurchaseRequisition(id, data),
  reject: (id, data) => rejectPurchaseRequisition(id, data),
  close: (id) => closePurchaseRequisition(id),
  remove: (id) => deletePurchaseRequisition(id),
  progress: (id) => getPurchaseRequisitionApproval(id),
  progressKey: (id) => getGetPurchaseRequisitionApprovalQueryKey(id),
};
