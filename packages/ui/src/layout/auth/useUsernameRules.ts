import type { Rule } from 'antd/es/form';
import { useTranslation } from 'react-i18next';
import { USERNAME_MAX_LENGTH, USERNAME_MIN_LENGTH, USERNAME_PATTERN } from './sign-in-rules';

export function useUsernameRules(): Rule[] {
  const { t } = useTranslation();
  return [
    { required: true, whitespace: true, message: t('userProfile.usernameRequired') },
    { pattern: USERNAME_PATTERN, message: t('userProfile.usernameFormat') },
    {
      min: USERNAME_MIN_LENGTH,
      max: USERNAME_MAX_LENGTH,
      message: t('userProfile.usernameFormat'),
    },
  ];
}
