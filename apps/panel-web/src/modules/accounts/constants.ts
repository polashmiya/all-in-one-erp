export const ACCOUNT_PATHS = {
  list: '/accounts',
  create: '/accounts/new',
  configuration: (id: string) => `/accounts/${id}/configuration`,
  lookups: (id: string) => `/accounts/${id}/lookups`,
};
