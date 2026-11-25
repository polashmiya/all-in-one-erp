import {
  approveJournal,
  cancelJournal,
  deleteJournal,
  getGetJournalApprovalQueryKey,
  getJournalApproval,
  rejectJournal,
  type JournalDetail,
  type SaveJournalLine,
  type SaveJournalRequest,
} from '@erp/api-client';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { trimmed } from '@/common/master-data';

export type JournalLineValue = Partial<SaveJournalLine>;

export type JournalValues = Omit<SaveJournalRequest, 'lines'> & { lines: JournalLineValue[] };

export function newJournal(): Partial<JournalValues> {
  return { journalDate: formatDate(new Date(), API_DATE_FORMAT), lines: [{}, {}] };
}

export function fromDetail(detail: JournalDetail): JournalValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
      chartOfAccountId: line.chartOfAccountId,
      supplierId: line.supplierId,
      customerId: line.customerId,
      debit: line.debit || null,
      credit: line.credit || null,
      remarks: line.remarks,
    })),
  };
}

export function toRequest(v: JournalValues): SaveJournalRequest {
  return {
    code: trimmed(v.code),
    journalDate: v.journalDate,
    narration: v.narration.trim(),
    lines: v.lines.map((line) => ({
      chartOfAccountId: line.chartOfAccountId ?? 0,
      supplierId: line.supplierId ?? null,
      customerId: line.customerId ?? null,
      debit: line.debit ?? null,
      credit: line.credit ?? null,
      remarks: trimmed(line.remarks),
    })),
    customData: v.customData ?? null,
  };
}

export function journalTotals(lines: JournalLineValue[]) {
  const sum = (side: 'debit' | 'credit') =>
    lines.reduce((total, line) => total + Math.round((line?.[side] ?? 0) * 100), 0) / 100;
  const debit = sum('debit');
  const credit = sum('credit');
  return { debit, credit, difference: Math.round((debit - credit) * 100) / 100 };
}

export const journalCalls: ApprovalCalls = {
  approve: (id, data) => approveJournal(id, data),
  reject: (id, data) => rejectJournal(id, data),
  cancel: (id, data) => cancelJournal(id, data),
  remove: (id) => deleteJournal(id),
  canCancel: (target) => !!target.isManual,
  progress: (id) => getJournalApproval(id),
  progressKey: (id) => getGetJournalApprovalQueryKey(id),
};
