import type { RouteObject } from 'react-router';
import { accountLedgerRoutes } from './reports/account-ledger/routes';
import { balanceSheetRoutes } from './reports/balance-sheet/routes';
import { incomeStatementRoutes } from './reports/income-statement/routes';
import { supplierLedgerRoutes } from './reports/supplier-ledger/routes';
import { trialBalanceRoutes } from './reports/trial-balance/routes';
import { chartOfAccountsRoutes } from './setup/chart-of-accounts/routes';
import { postingAccountsRoutes } from './setup/posting-accounts/routes';
import { journalRoutes } from './transactions/journal/routes';

export const accountingRoutes: RouteObject[] = [
  ...chartOfAccountsRoutes,
  ...postingAccountsRoutes,
  ...journalRoutes,
  ...accountLedgerRoutes,
  ...supplierLedgerRoutes,
  ...trialBalanceRoutes,
  ...incomeStatementRoutes,
  ...balanceSheetRoutes,
];
