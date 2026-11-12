import './accounts.css';
import type { RouteObject } from 'react-router';

export const accountsRoutes: RouteObject[] = [
  {
    path: 'accounts',
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./account-list/pages/AccountsPage')).AccountsPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./account-setup/pages/AccountSetupPage')).AccountSetupPage,
        }),
      },
      {
        path: ':id/configuration',
        lazy: async () => ({
          Component: (await import('./account-configuration/pages/AccountConfigurationPage'))
            .AccountConfigurationPage,
        }),
      },
      {
        path: ':id/layout',
        lazy: async () => ({
          Component: (await import('./account-layout/pages/AccountLayoutPage')).AccountLayoutPage,
        }),
      },
      {
        path: ':id/history',
        lazy: async () => ({
          Component: (await import('./account-history/pages/AccountHistoryPage'))
            .AccountHistoryPage,
        }),
      },
      {
        path: ':id/lookups',
        lazy: async () => ({
          Component: (await import('./account-lookups/pages/AccountLookupsPage'))
            .AccountLookupsPage,
        }),
      },
    ],
  },
];
