export const ERP_USER_PATHS = {
  list: '/erp-users',
  detail: (id: string) => `/erp-users/${id}`,
  forAccount: (accountId: string) => `/erp-users?account=${accountId}`,
};

export const USER_TYPES = ['admins', 'users'] as const;

export const USER_VIEWS = ['all', 'active', 'inactive', 'locked'] as const;

export const SUPER_ADMIN_ROLE = 'SuperAdmin';

export const ACCOUNT_ADMINS_PAGE_SIZE = 100;
