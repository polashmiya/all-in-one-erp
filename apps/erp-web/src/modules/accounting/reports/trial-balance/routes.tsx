import type { RouteObject } from 'react-router';
import { TRIAL_BALANCE_PATH } from './constants';

export const trialBalanceRoutes: RouteObject[] = [
  {
    path: TRIAL_BALANCE_PATH.slice(1),
    lazy: async () => ({ Component: (await import('./pages/TrialBalancePage')).TrialBalancePage }),
  },
];
