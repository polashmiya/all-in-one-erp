import { USER_ROLE_PATH } from '../user-management';

type Id = string;

export const rolePaths = {
  list: USER_ROLE_PATH,
  create: `${USER_ROLE_PATH}/new`,
  edit: (id: Id) => `${USER_ROLE_PATH}/${encodeURIComponent(id)}`,
};

export const STANDARD_ACTIONS = ['view', 'create', 'edit', 'delete', 'export', 'import'] as const;

export const VIEW_ACTION = 'view';

export const ROLE_NAME_MAX = 100;
export const ROLE_DESCRIPTION_MAX = 500;
