import {
  approveTaxDeposit,
  cancelTaxDeposit,
  deleteTaxDeposit,
  getGetTaxDepositApprovalQueryKey,
  getTaxDepositApproval,
  rejectTaxDeposit,
  type PendingTax,
  type SaveTaxDepositLine,
  type SaveTaxDepositRequest,
  type TaxDepositDetail,
} from '@erp/api-client';
import type { LookupItem } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed } from '@/common/master-data';
import { usePayFrom } from '@/common/pay-from';
import { DEPOSIT_LOOKUP_LISTS, DEPOSIT_LOOKUPS, TAX_TYPES } from './constants';

export interface DepositLine extends Partial<SaveTaxDepositLine> {
  paymentCode?: string;
  paymentDate?: string;
  supplierName?: string | null;
  deducted?: number;
  remaining?: number;
}

export type DepositValues = Omit<SaveTaxDepositRequest, 'lines'> & { lines: DepositLine[] };

export function useTaxTypes(): LookupItem[] {
  const { t } = useTranslation();
  return TAX_TYPES.map((id) => ({ id, name: t(`deposit.type.${id}`), isActive: true }));
}

export function useDepositLookups(enabled = true) {
  const types = useTaxTypes();
  const lists = useLookupLists(DEPOSIT_LOOKUP_LISTS, enabled);
  const payFrom = usePayFrom(lists[DEPOSIT_LOOKUPS.method], enabled);
  return {
    types,
    payFrom,
    lookups: { ...lists, ...payFrom.lookups, [DEPOSIT_LOOKUPS.type]: types },
  };
}

export function newDeposit(): Partial<DepositValues> {
  return { depositDate: formatDate(new Date(), API_DATE_FORMAT), lines: [] };
}

export const fromTax = (tax: PendingTax): DepositLine => ({
  purchasePaymentId: tax.purchasePaymentId,
  paymentCode: tax.paymentCode,
  paymentDate: tax.paymentDate,
  supplierName: tax.supplierName,
  deducted: tax.deducted,
  remaining: tax.remaining,
  amount: tax.remaining,
});

export function fromDetail(detail: TaxDepositDetail): DepositValues {
  return { ...detail, lines: detail.lines.map((line) => ({ ...line })) };
}

export function toRequest(v: DepositValues): SaveTaxDepositRequest {
  return {
    code: trimmed(v.code),
    depositDate: v.depositDate,
    type: v.type,
    paymentMethodId: v.paymentMethodId ?? null,
    bankAccountId: v.bankAccountId ?? null,
    challanNo: trimmed(v.challanNo),
    challanDate: v.challanDate ?? null,
    remarks: trimmed(v.remarks),
    lines: v.lines.map((line) => ({
      purchasePaymentId: line.purchasePaymentId ?? 0,
      amount: line.amount ?? 0,
    })),
    customData: v.customData ?? null,
  };
}

export const depositTotal = (lines: DepositLine[]) =>
  lines.reduce((sum, line) => sum + (line?.amount ?? 0), 0);

export const depositCalls: ApprovalCalls = {
  approve: (id, data) => approveTaxDeposit(id, data),
  reject: (id, data) => rejectTaxDeposit(id, data),
  cancel: (id, data) => cancelTaxDeposit(id, data),
  remove: (id) => deleteTaxDeposit(id),
  progress: (id) => getTaxDepositApproval(id),
  progressKey: (id) => getGetTaxDepositApprovalQueryKey(id),
};
