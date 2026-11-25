import type { RouteObject } from 'react-router';
import { CUSTOMER_DUE_PATH } from './constants';

export const customerDueRoutes: RouteObject[] = [
  {
    path: CUSTOMER_DUE_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/CustomerDuePage')).CustomerDuePage,
    }),
  },
];
