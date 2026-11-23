import type { RouteObject } from 'react-router';
import { MY_APPROVALS_PATH } from './constants';

export const myApprovalsRoutes: RouteObject[] = [
  {
    path: MY_APPROVALS_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/MyApprovalsPage')).MyApprovalsPage,
    }),
  },
];
