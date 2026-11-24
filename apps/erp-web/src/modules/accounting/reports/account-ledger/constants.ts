import { featurePath } from '@/common/constants/routes';
import type { DateRange } from '@/common/reports/report';
import { ACCOUNTING_MODULE } from '../../accounting';

export const ACCOUNT_LEDGER_FEATURE = 'accounting.account-ledger';
export const ACCOUNT_LEDGER_PATH = featurePath(ACCOUNTING_MODULE, 'account-ledger');

export const accountLedgerLink = (accountId: number, { from, to }: DateRange) =>
  `${ACCOUNT_LEDGER_PATH}?${new URLSearchParams({
    accountId: String(accountId),
    ...(from ? { from } : {}),
    ...(to ? { to } : {}),
  })}`;
