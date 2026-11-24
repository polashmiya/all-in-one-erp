import type { RouteObject } from 'react-router';
import { accountLedgerRoutes } from './reports/account-ledger/routes';
import { balanceSheetRoutes } from './reports/balance-sheet/routes';
import { incomeStatementRoutes } from './reports/income-statement/routes';
import { supplierLedgerRoutes } from './reports/supplier-ledger/routes';
import { trialBalanceRoutes } from './reports/trial-balance/routes';
import { chartOfAccountsRoutes } from './setup/chart-of-accounts/routes';
import { postingAccountsRoutes } from './setup/posting-accounts/routes';
import { periodClosingRoutes } from './setup/period-closing/routes';
import { bankAccountsRoutes } from './setup/bank-accounts/routes';
import { taxDepositRoutes } from './transactions/tax-deposit/routes';
import { journalRoutes } from './transactions/journal/routes';

export const accountingRoutes: RouteObject[] = [
  ...chartOfAccountsRoutes,
  ...bankAccountsRoutes,
  ...postingAccountsRoutes,
  ...periodClosingRoutes,
  ...taxDepositRoutes,
  ...journalRoutes,
  ...accountLedgerRoutes,
  ...supplierLedgerRoutes,
  ...trialBalanceRoutes,
  ...incomeStatementRoutes,
  ...balanceSheetRoutes,
];
