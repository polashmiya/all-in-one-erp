export const PASSWORD_MIN_LENGTH = 8;

export const passwordRules = [
  { key: 'auth.passwordRuleLength', test: (value: string) => value.length >= PASSWORD_MIN_LENGTH },
  { key: 'auth.passwordRuleUpper', test: (value: string) => /[A-Z]/.test(value) },
  { key: 'auth.passwordRuleLower', test: (value: string) => /[a-z]/.test(value) },
  { key: 'auth.passwordRuleDigit', test: (value: string) => /[0-9]/.test(value) },
];

export const isStrongPassword = (value: string) => passwordRules.every((rule) => rule.test(value));

export const USERNAME_MIN_LENGTH = 3;
export const USERNAME_MAX_LENGTH = 50;
export const USERNAME_PATTERN = /^[a-zA-Z0-9._-]+$/;

export const suggestUsername = (email: string) =>
  (email.split('@')[0] ?? '')
    .toLowerCase()
    .replace(/[^a-z0-9._-]/g, '')
    .slice(0, USERNAME_MAX_LENGTH);

export const FULL_NAME_MAX_LENGTH = 200;
export const EMAIL_MAX_LENGTH = 256;
export const PHONE_MAX_LENGTH = 32;
