import { LockOutlined } from '@ant-design/icons';
import { Alert, Button, Form, Input, type FormInstance } from 'antd';
import { useTranslation } from 'react-i18next';
import { NewPasswordFields } from './PasswordFields';

export interface ChangePasswordValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ChangePasswordFormProps {
  form: FormInstance<ChangePasswordValues>;
  onSubmit: (values: ChangePasswordValues) => Promise<unknown>;
  loading?: boolean;
  error?: string | null;
  onCancel?: () => void;
}

export function ChangePasswordForm({
  form,
  onSubmit,
  loading,
  error,
  onCancel,
}: ChangePasswordFormProps) {
  const { t } = useTranslation();

  return (
    <Form<ChangePasswordValues>
      form={form}
      layout="vertical"
      requiredMark={false}
      onFinish={onSubmit}
      className="erp-form erp-auth__form"
      disabled={loading}
    >
      {error && <Alert type="error" showIcon message={error} className="erp-auth__alert" />}
      <Form.Item
        name="currentPassword"
        label={t('auth.currentPassword')}
        rules={[{ required: true, message: t('auth.passwordRequired') }]}
      >
        <Input.Password
          size="large"
          prefix={<LockOutlined />}
          autoComplete="current-password"
          autoFocus
        />
      </Form.Item>
      <NewPasswordFields size="large" />
      <Button
        type="primary"
        htmlType="submit"
        size="large"
        block
        loading={loading}
        className="erp-auth__submit"
      >
        {t('auth.changePasswordSubmit')}
      </Button>
      {onCancel && (
        <Button type="link" block onClick={onCancel}>
          {t('common.back')}
        </Button>
      )}
    </Form>
  );
}
