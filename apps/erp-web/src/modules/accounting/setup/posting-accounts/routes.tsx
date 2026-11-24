import type { RouteObject } from 'react-router';
import { POSTING_PATH } from './constants';

export const postingAccountsRoutes: RouteObject[] = [
  {
    path: POSTING_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/PostingAccountsPage')).PostingAccountsPage,
    }),
  },
];
