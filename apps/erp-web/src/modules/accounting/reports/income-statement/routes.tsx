import type { RouteObject } from 'react-router';
import { INCOME_STATEMENT_PATH } from './constants';

export const incomeStatementRoutes: RouteObject[] = [
  {
    path: INCOME_STATEMENT_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/IncomeStatementPage')).IncomeStatementPage,
    }),
  },
];
