export const ACCOUNT_PATHS = {
  list: '/accounts',
  create: '/accounts/new',
  configuration: (id: string) => `/accounts/${id}/configuration`,
  layout: (id: string) => `/accounts/${id}/layout`,
  lookups: (id: string) => `/accounts/${id}/lookups`,
};

export const SCREEN_PARAMS = { feature: 'feature', unit: 'unit' };
