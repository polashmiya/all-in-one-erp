import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { featurePath } from '@/common/constants/routes';

export const USER_MANAGEMENT_MODULE = 'system-setup';
export const USER_MANAGEMENT_API = '/api/erp/user-management';

export const USER_FEATURE = 'system-setup.user';
export const EMPLOYEE_FEATURE = 'system-setup.employee';
export const USER_ROLE_FEATURE = 'system-setup.user-role';

export const USER_PATH = featurePath(USER_MANAGEMENT_MODULE, 'user');
export const EMPLOYEE_PATH = featurePath(USER_MANAGEMENT_MODULE, 'employee');
export const USER_ROLE_PATH = featurePath(USER_MANAGEMENT_MODULE, 'user-role');

type Id = number | string;

export const userPaths = {
  list: USER_PATH,
  create: `${USER_PATH}/new`,
  createFor: (employeeId: Id) => `${USER_PATH}/new?employee=${encodeURIComponent(employeeId)}`,
  edit: (id: Id) => `${USER_PATH}/${encodeURIComponent(id)}/edit`,
};

export const employeePaths = {
  list: EMPLOYEE_PATH,
  create: `${EMPLOYEE_PATH}/new`,
  view: (id: Id) => `${EMPLOYEE_PATH}/${encodeURIComponent(id)}`,
  edit: (id: Id) => `${EMPLOYEE_PATH}/${encodeURIComponent(id)}/edit`,
};

export function useRefreshUserManagement() {
  const client = useQueryClient();
  return useCallback(
    () =>
      client.invalidateQueries({
        predicate: ({ queryKey }) =>
          typeof queryKey[0] === 'string' && queryKey[0].startsWith(USER_MANAGEMENT_API),
      }),
    [client],
  );
}
