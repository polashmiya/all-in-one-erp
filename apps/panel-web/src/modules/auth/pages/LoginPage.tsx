import { errorStatus, panelLogin, type LoginRequest } from '@erp/api-client';
import { AuthHeading, SignInForm, useGreeting } from '@erp/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { FORGOT_PASSWORD_PATH } from '@/common/constants/routes';
import { useAuthStore } from '@/common/store/auth-store';
import { AuthLayout } from '../components/AuthLayout';

const ERROR_KEYS: Record<number, string> = {
  0: 'auth.serverUnreachable',
  401: 'auth.invalidCredentials',
  429: 'auth.tooManyAttempts',
};

export function LoginPage() {
  const { t } = useTranslation();
  const greeting = useGreeting();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const signIn = useAuthStore((s) => s.signIn);

  const submit = async (values: LoginRequest) => {
    setError(null);
    setLoading(true);
    try {
      signIn(await panelLogin(values));
    } catch (e) {
      setError(t(ERROR_KEYS[errorStatus(e)] ?? 'auth.unexpected'));
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
        onForgotPassword={() => navigate(FORGOT_PASSWORD_PATH)}
        footer={t('login.internalOnly')}
      />
    </AuthLayout>
  );
}
