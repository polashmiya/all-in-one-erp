import { applyFieldErrors, erpChangePassword } from '@erp/api-client';
import { AuthHeading, ChangePasswordForm, type ChangePasswordValues } from '@erp/ui';
import { App, Form } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router';
import { redirectTarget } from '@/common/guards/redirect';
import { useAuthStore } from '@/common/store/auth-store';
import { AuthLayout } from '../components/AuthLayout';

export function ChangePasswordPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const location = useLocation();
  const [form] = Form.useForm<ChangePasswordValues>();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const forced = useAuthStore((s) => s.mustChangePassword);
  const setTokens = useAuthStore((s) => s.setTokens);

  const submit = async ({ currentPassword, newPassword }: ChangePasswordValues) => {
    setError(null);
    setLoading(true);
    try {
      setTokens(await erpChangePassword({ currentPassword, newPassword }));
      void message.success(t('auth.passwordChanged'));
      navigate(redirectTarget(location.state), { replace: true });
    } catch (e) {
      if (!applyFieldErrors(form, e)) setError(t('auth.changePasswordFailed'));
      setLoading(false);
    }
  };

  return (
    <AuthLayout>
      <AuthHeading
        title={t('auth.changePasswordTitle')}
        lead={forced ? t('auth.changePasswordForced') : t('auth.changePasswordLead')}
      />
      <ChangePasswordForm
        form={form}
        onSubmit={submit}
        loading={loading}
        error={error}
        onCancel={forced ? undefined : () => navigate(-1)}
      />
    </AuthLayout>
  );
}
