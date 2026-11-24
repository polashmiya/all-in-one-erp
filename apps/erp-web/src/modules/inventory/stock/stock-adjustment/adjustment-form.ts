import {
  approveStockAdjustment,
  deleteStockAdjustment,
  getGetStockAdjustmentApprovalQueryKey,
  getStockAdjustmentApproval,
  rejectStockAdjustment,
  type SaveStockAdjustmentLine,
  type SaveStockAdjustmentRequest,
  type StockAdjustmentDetail,
} from '@erp/api-client';
import type { LookupItem, LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useUnitWarehouses } from '@/common/documents/documents';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed } from '@/common/master-data';
import { ADJUSTMENT_LOOKUP_LISTS, ADJUSTMENT_LOOKUPS, ADJUSTMENT_TYPES } from './constants';

export interface AdjustmentLineValue extends SaveStockAdjustmentLine {
  itemCode?: string;
  itemName?: string;
  uomName?: string;
}

export type AdjustmentLine = Partial<AdjustmentLineValue>;

export type AdjustmentValues = Omit<SaveStockAdjustmentRequest, 'lines'> & {
  lines: AdjustmentLine[];
};

export function useAdjustmentLookups(enabled = true): LookupSources {
  const { t } = useTranslation();
  const lists = useLookupLists(ADJUSTMENT_LOOKUP_LISTS, enabled);
  const warehouses = useUnitWarehouses(enabled);
  const types: LookupItem[] = ADJUSTMENT_TYPES.map((id) => ({
    id,
    name: t(`adjustment.type.${id}`),
    isActive: true,
  }));

  return {
    ...lists,
    [ADJUSTMENT_LOOKUPS.type]: types,
    [ADJUSTMENT_LOOKUPS.warehouse]: warehouses,
  };
}

export const emptyLine = (): AdjustmentLine => ({ quantity: 1, rate: null });

export const lineAmount = (line?: AdjustmentLine, cost?: number) =>
  (line?.quantity ?? 0) * (cost ?? line?.rate ?? 0);

export function newAdjustment(): Partial<AdjustmentValues> {
  return { adjustmentDate: formatDate(new Date(), API_DATE_FORMAT), lines: [emptyLine()] };
}

export function fromDetail(detail: StockAdjustmentDetail): AdjustmentValues {
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

export function toRequest(v: AdjustmentValues): SaveStockAdjustmentRequest {
  return {
    code: trimmed(v.code),
    adjustmentDate: v.adjustmentDate,
    type: v.type,
    warehouseId: v.warehouseId,
    reasonId: v.reasonId ?? null,
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

export const adjustmentCalls: ApprovalCalls = {
  approve: (id, data) => approveStockAdjustment(id, data),
  reject: (id, data) => rejectStockAdjustment(id, data),
  remove: (id) => deleteStockAdjustment(id),
  progress: (id) => getStockAdjustmentApproval(id),
  progressKey: (id) => getGetStockAdjustmentApprovalQueryKey(id),
};
