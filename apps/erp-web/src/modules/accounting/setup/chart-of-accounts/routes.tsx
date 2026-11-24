import type { RouteObject } from 'react-router';
import { CHART_PATH } from './constants';

export const chartOfAccountsRoutes: RouteObject[] = [
  {
    path: CHART_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/ChartOfAccountsPage')).ChartOfAccountsPage,
    }),
  },
];
