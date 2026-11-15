import { erpForgotPassword, erpResetPassword, errorStatus, problemMessage } from '@erp/api-client';
import { ForgotPasswordFlow, type ResetPasswordValues } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router';
import { HOME_PATH, LOGIN_PATH } from '@/common/constants/routes';
import { endSession } from '@/common/services/api-client';
import { isSignedIn, useAuthStore } from '@/common/store/auth-store';
import { AuthLayout } from '../components/AuthLayout';

export function ForgotPasswordPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const signedIn = useAuthStore(isSignedIn);

  const reset = async (values: ResetPasswordValues) => {
    try {
      await erpResetPassword(values);
      return null;
    } catch (e) {
      if (errorStatus(e) === 400) return problemMessage(e, t('auth.resetFailed'));
      throw e;
    }
  };

  const done = () => {
    if (signedIn) endSession();
    navigate(LOGIN_PATH, { replace: true });
  };

  return (
    <AuthLayout>
      <ForgotPasswordFlow
        allowUsername
        initialLogin={params.get('login') ?? undefined}
        onSendCode={(login) => erpForgotPassword({ login, language: i18n.language })}
        onReset={reset}
        onBack={() => navigate(signedIn ? HOME_PATH : LOGIN_PATH)}
        onDone={done}
      />
    </AuthLayout>
  );
}
