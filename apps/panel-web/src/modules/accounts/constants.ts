export const ACCOUNT_PATHS = {
  list: '/accounts',
  create: '/accounts/new',
  workspace: '/configuration',
  detail: (id: string) => `/accounts/${id}`,
  edit: (id: string) => `/accounts/${id}/edit`,
  theme: (id: string) => `/accounts/${id}/theme`,
  configuration: (id: string) => `/accounts/${id}/configuration`,
  layout: (id: string) => `/accounts/${id}/layout`,
  history: (id: string) => `/accounts/${id}/history`,
  lookups: (id: string) => `/accounts/${id}/lookups`,
};

export const SCREEN_PARAMS = { feature: 'feature', unit: 'unit' };

export const WORKSPACE_PARAM = 'account';

export const ACCOUNT_PICKER_SIZE = 100;
