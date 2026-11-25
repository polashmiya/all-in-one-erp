import type { RouteObject } from 'react-router';
import { COLLECTION_REPORT_PATH } from './constants';

export const collectionReportRoutes: RouteObject[] = [
  {
    path: COLLECTION_REPORT_PATH.slice(1),
    lazy: async () => ({
      Component: (await import('./pages/CollectionReportPage')).CollectionReportPage,
    }),
  },
];
