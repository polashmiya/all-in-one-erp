import type { RouteObject } from 'react-router';
import { DELIVERY_PATH } from './constants';

export const salesDeliveryRoutes: RouteObject[] = [
  {
    path: DELIVERY_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/DeliveryListPage')).DeliveryListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/DeliveryFormPage')).DeliveryFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/DeliveryViewPage')).DeliveryViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/DeliveryFormPage')).DeliveryFormPage,
        }),
      },
    ],
  },
];
