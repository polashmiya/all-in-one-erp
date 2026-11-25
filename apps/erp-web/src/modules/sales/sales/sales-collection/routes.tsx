import type { RouteObject } from 'react-router';
import { COLLECTION_PATH } from './constants';

export const salesCollectionRoutes: RouteObject[] = [
  {
    path: COLLECTION_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/CollectionListPage')).CollectionListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/CollectionFormPage')).CollectionFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/CollectionViewPage')).CollectionViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/CollectionFormPage')).CollectionFormPage,
        }),
      },
    ],
  },
];
