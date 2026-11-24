import {
  approveStockTransfer,
  deleteStockTransfer,
  getGetStockTransferApprovalQueryKey,
  getStockTransferApproval,
  rejectStockTransfer,
  type SaveStockTransferLine,
  type SaveStockTransferRequest,
  type StockTransferDetail,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useUnitWarehouses } from '@/common/documents/documents';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { trimmed } from '@/common/master-data';
import { TRANSFER_LOOKUPS } from './constants';

export interface TransferLineValue extends SaveStockTransferLine {
  itemCode?: string;
  itemName?: string;
  uomName?: string;
}

export type TransferLine = Partial<TransferLineValue>;

export type TransferValues = Omit<SaveStockTransferRequest, 'lines'> & { lines: TransferLine[] };

export function useTransferLookups(enabled = true): LookupSources {
  const warehouses = useUnitWarehouses(enabled);
  return { [TRANSFER_LOOKUPS.warehouse]: warehouses };
}

export const emptyLine = (): TransferLine => ({ quantity: 1 });

export function newTransfer(): Partial<TransferValues> {
  return { transferDate: formatDate(new Date(), API_DATE_FORMAT), lines: [emptyLine()] };
}

export function fromDetail(detail: StockTransferDetail): TransferValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      itemId: line.itemId,
      itemCode: line.itemCode,
      itemName: line.itemName,
      uomName: line.uomName,
      quantity: line.quantity,
      remarks: line.remarks,
    })),
  };
}

export function toRequest(v: TransferValues): SaveStockTransferRequest {
  return {
    code: trimmed(v.code),
    transferDate: v.transferDate,
    fromWarehouseId: v.fromWarehouseId,
    toWarehouseId: v.toWarehouseId,
    remarks: trimmed(v.remarks),
    lines: v.lines.map((line) => ({
      itemId: line.itemId ?? 0,
      quantity: line.quantity ?? 0,
      remarks: trimmed(line.remarks),
    })),
    customData: v.customData ?? null,
  };
}

export const transferCalls: ApprovalCalls = {
  approve: (id, data) => approveStockTransfer(id, data),
  reject: (id, data) => rejectStockTransfer(id, data),
  remove: (id) => deleteStockTransfer(id),
  progress: (id) => getStockTransferApproval(id),
  progressKey: (id) => getGetStockTransferApprovalQueryKey(id),
};
