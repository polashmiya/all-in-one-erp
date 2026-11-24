import type { RouteObject } from 'react-router';
import { PERIOD_CLOSING_PATH } from './constants';

export const periodClosingRoutes: RouteObject[] = [
  {
    path: PERIOD_CLOSING_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/PeriodClosingPage')).PeriodClosingPage,
    }),
  },
];
