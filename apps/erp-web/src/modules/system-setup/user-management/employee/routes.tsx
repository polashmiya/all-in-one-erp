import type { RouteObject } from 'react-router';
import { EMPLOYEE_PATH } from '../user-management';

export const employeeRoutes: RouteObject[] = [
  {
    path: EMPLOYEE_PATH.slice(1),
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import('./pages/EmployeeListPage')).EmployeeListPage,
        }),
      },
      {
        path: 'new',
        lazy: async () => ({
          Component: (await import('./pages/EmployeeFormPage')).EmployeeFormPage,
        }),
      },
      {
        path: ':id',
        lazy: async () => ({
          Component: (await import('./pages/EmployeeViewPage')).EmployeeViewPage,
        }),
      },
      {
        path: ':id/edit',
        lazy: async () => ({
          Component: (await import('./pages/EmployeeFormPage')).EmployeeFormPage,
        }),
      },
    ],
  },
];
