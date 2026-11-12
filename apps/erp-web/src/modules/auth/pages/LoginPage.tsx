import {
  erpLogin,
  errorStatus,
  selectBusinessUnit,
  type ApiError,
  type LoginRequest,
} from '@erp/api-client';
import { AuthHeading, SignInForm, useGreeting } from '@erp/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useAuthStore } from '@/common/store/auth-store';
import { applyAccountTheme } from '@/common/utils/account-theme';
import { AuthLayout } from '../components/AuthLayout';

const ERROR_KEYS: Record<number, string> = {
  0: 'auth.serverUnreachable',
  401: 'auth.invalidCredentials',
  429: 'auth.tooManyAttempts',
};

export function LoginPage() {
  const { t } = useTranslation();
  const greeting = useGreeting();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const signIn = useAuthStore((s) => s.signIn);
  const selectUnit = useAuthStore((s) => s.selectUnit);

  const submit = async (values: LoginRequest) => {
    setError(null);
    setLoading(true);
    try {
      const login = await erpLogin(values);
      const only = login.businessUnits.length === 1 ? login.businessUnits[0] : undefined;
      const selection = only
        ? await selectBusinessUnit(
            { businessUnitId: only.id },
            { headers: { Authorization: `Bearer ${login.accessToken}` } },
          )
        : undefined;
      signIn(login);
      if (login.theme) applyAccountTheme(login.theme);
      if (selection) selectUnit(selection);
    } catch (e) {
      const status = errorStatus(e);
      setError(
        status === 403
          ? ((e as ApiError).problem?.detail ?? t('auth.unexpected'))
          : t(ERROR_KEYS[status] ?? 'auth.unexpected'),
      );
      setLoading(false);
    }
  };

  return (
    <AuthLayout>
      <AuthHeading
        eyebrow={greeting}
        title={t('login.welcome')}
        lead={t('login.lead', { app: appConfig.name })}
      />
      <SignInForm
        onSubmit={submit}
        error={error}
        loading={loading}
        demo={appConfig.demoAccount}
        footer={t('login.askAdmin')}
      />
    </AuthLayout>
  );
}
