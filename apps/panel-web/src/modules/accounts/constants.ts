export const ACCOUNT_PATHS = {
  list: '/accounts',
  create: '/accounts/new',
  workspace: '/configuration',
  featuresOf: (id: string) => `/feature-permissions/${id}`,
  detail: (id: string) => `/accounts/${id}`,
  edit: (id: string) => `/accounts/${id}/edit`,
  theme: (id: string) => `/accounts/${id}/theme`,
  configuration: (id: string) => `/accounts/${id}/configuration`,
  layout: (id: string) => `/accounts/${id}/layout`,
  history: (id: string) => `/accounts/${id}/history`,
  lookups: (id: string) => `/accounts/${id}/lookups`,
  approvals: (id: string) => `/accounts/${id}/approvals`,
};

export const SCREEN_PARAMS = { feature: 'feature', unit: 'unit' };

export const WORKSPACE_PARAM = 'account';

export const ACCOUNT_PICKER_SIZE = 100;

export const ACCOUNT_DRAFTS = {
  setup: 'accounts.setup',
  lookupValue: (accountId: string, type: string) => `accounts.lookups.${accountId}.${type}`,
  customField: (accountId: string, feature: string) => `accounts.fields.${accountId}.${feature}`,
};
