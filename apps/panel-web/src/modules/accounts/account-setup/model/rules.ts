import { checkAccountAvailability } from '@erp/api-client';
import { USERNAME_MAX_LENGTH, USERNAME_MIN_LENGTH, USERNAME_PATTERN } from '@erp/ui';
import type { FormInstance, FormRule } from 'antd';
import type { useTranslation } from 'react-i18next';
import {
  CODE_PATTERN,
  isDuplicateCode,
  PHONE_PATTERN,
  STRONG_PASSWORD,
  suggestCode,
  type FieldPath,
} from './setup-model';

type TFunction = ReturnType<typeof useTranslation>['t'];

export function setupRules(t: TFunction) {
  return {
    required: { required: true, whitespace: true } satisfies FormRule,
    code: [
      { required: true },
      { pattern: CODE_PATTERN, message: t('setup.codeFormat') },
      { max: 32 },
    ] satisfies FormRule[],
    phone: { pattern: PHONE_PATTERN, message: t('setup.phoneFormat') } satisfies FormRule,
    email: { type: 'email' } satisfies FormRule,
    website: { pattern: /^https?:\/\/\S+$/i, message: t('setup.websiteFormat') } satisfies FormRule,
    strongPassword: {
      pattern: STRONG_PASSWORD,
      message: t('setup.passwordRule'),
    } satisfies FormRule,

    uniqueIn:
      (listPath: FieldPath, index: number): FormRule =>
      ({ getFieldValue }) => ({
        validator: async () => {
          const items = (getFieldValue(listPath) as { code?: string }[] | undefined) ?? [];
          if (
            isDuplicateCode(
              items.map((item) => item?.code),
              index,
            )
          )
            throw new Error(t('setup.duplicateCode'));
        },
      }),

    accountCodeAvailable: {
      validator: async (_: unknown, value?: string) => {
        if (!value || value.length < 2 || !CODE_PATTERN.test(value)) return;
        const { accountCodeAvailable } = await checkAccountAvailability({ accountCode: value });
        if (accountCodeAvailable === false) throw new Error(t('setup.codeTaken'));
      },
    } satisfies FormRule,

    username: [
      { required: true, whitespace: true },
      { pattern: USERNAME_PATTERN, message: t('setup.usernameFormat') },
      { min: USERNAME_MIN_LENGTH, max: USERNAME_MAX_LENGTH, message: t('setup.usernameFormat') },
    ] satisfies FormRule[],

    adminUsernameAvailable: {
      validator: async (_: unknown, value?: string) => {
        if (!value || value.length < USERNAME_MIN_LENGTH || !USERNAME_PATTERN.test(value)) return;
        const { adminUsernameAvailable } = await checkAccountAvailability({ adminUsername: value });
        if (adminUsernameAvailable === false) throw new Error(t('setup.usernameTaken'));
      },
    } satisfies FormRule,

    adminEmailAvailable: {
      validator: async (_: unknown, value?: string) => {
        if (!value || !/^\S+@\S+\.\S+$/.test(value)) return;
        const { adminEmailAvailable } = await checkAccountAvailability({ adminEmail: value });
        if (adminEmailAvailable === false) throw new Error(t('setup.emailTaken'));
      },
    } satisfies FormRule,
  };
}

export function suggestCodeOnBlur(form: FormInstance, itemPath: FieldPath, maxLength = 12) {
  return (event: { target: { value: string } }) => {
    if (form.getFieldValue([...itemPath, 'code'])) return;
    const suggestion = suggestCode(event.target.value, maxLength);
    if (suggestion) form.setFieldValue([...itemPath, 'code'], suggestion);
  };
}
