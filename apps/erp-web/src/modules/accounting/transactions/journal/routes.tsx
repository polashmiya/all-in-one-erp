import type { RouteObject } from 'react-router';
import { JOURNAL_PATH } from './constants';

export const journalRoutes: RouteObject[] = [
  {
    path: JOURNAL_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/JournalListPage')).JournalListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/JournalFormPage')).JournalFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/JournalViewPage')).JournalViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/JournalFormPage')).JournalFormPage,
        }),
      },
    ],
  },
];
