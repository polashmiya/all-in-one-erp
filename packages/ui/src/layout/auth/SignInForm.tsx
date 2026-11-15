import {
  ArrowRightOutlined,
  KeyOutlined,
  LockOutlined,
  MailOutlined,
  UserOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import { Alert, Button, Form, Input, Typography } from 'antd';
import { useState, type KeyboardEvent, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

export interface SignInValues {
  login: string;
  password: string;
}

export interface SignInFormProps {
  onSubmit: (values: SignInValues) => Promise<unknown>;
  error?: string | null;
  loading?: boolean;
  demo?: SignInValues;
  footer?: ReactNode;
  allowUsername?: boolean;
  onForgotPassword?: () => void;
}

export function AuthHeading({
  title,
  lead,
  eyebrow,
}: {
  title: ReactNode;
  lead?: ReactNode;
  eyebrow?: ReactNode;
}) {
  return (
    <div className="erp-auth__heading">
      {eyebrow && <span className="erp-auth__eyebrow">{eyebrow}</span>}
      <h1>{title}</h1>
      {lead && <p>{lead}</p>}
    </div>
  );
}

function DemoHint({ demo, onUse }: { demo: SignInValues; onUse: () => void }) {
  const { t } = useTranslation();
  return (
    <div className="erp-auth__demo">
      <span className="erp-auth__demo-icon">
        <KeyOutlined />
      </span>
      <span className="erp-auth__demo-text">
        <span className="erp-auth__demo-title">{t('auth.demoAccount')}</span>
        <span className="erp-auth__demo-value">
          {demo.login} · {demo.password}
        </span>
      </span>
      <Button size="small" type="link" onClick={onUse}>
        {t('auth.useDemo')}
      </Button>
    </div>
  );
}

export function SignInForm({
  onSubmit,
  error,
  loading,
  demo,
  footer,
  allowUsername = false,
  onForgotPassword,
}: SignInFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SignInValues>();
  const [capsLock, setCapsLock] = useState(false);
  const trackCapsLock = (event: KeyboardEvent<HTMLInputElement>) =>
    setCapsLock(event.getModifierState('CapsLock'));

  return (
    <Form<SignInValues>
      form={form}
      layout="vertical"
      requiredMark={false}
      onFinish={onSubmit}
      className="erp-form erp-auth__form"
      disabled={loading}
    >
      {error && <Alert type="error" showIcon message={error} className="erp-auth__alert" />}
      <Form.Item
        name="login"
        label={allowUsername ? t('auth.login') : t('auth.email')}
        rules={[
          {
            required: true,
            message: allowUsername ? t('auth.loginRequired') : t('auth.emailRequired'),
          },
          ...(allowUsername ? [] : [{ type: 'email' as const, message: t('auth.emailInvalid') }]),
        ]}
      >
        <Input
          size="large"
          prefix={allowUsername ? <UserOutlined /> : <MailOutlined />}
          placeholder={allowUsername ? t('auth.loginPlaceholder') : t('auth.emailPlaceholder')}
          autoComplete="username"
          autoFocus
        />
      </Form.Item>
      <Form.Item
        name="password"
        label={t('auth.password')}
        rules={[{ required: true, message: t('auth.passwordRequired') }]}
        extra={
          capsLock && (
            <Typography.Text type="warning">
              <WarningOutlined /> {t('auth.capsOn')}
            </Typography.Text>
          )
        }
      >
        <Input.Password
          size="large"
          prefix={<LockOutlined />}
          placeholder={t('auth.passwordPlaceholder')}
          autoComplete="current-password"
          onKeyDown={trackCapsLock}
          onKeyUp={trackCapsLock}
        />
      </Form.Item>
      {onForgotPassword && (
        <div className="erp-auth__forgot">
          <Button type="link" size="small" onClick={onForgotPassword}>
            {t('auth.forgotPassword')}
          </Button>
        </div>
      )}
      {demo && <DemoHint demo={demo} onUse={() => form.setFieldsValue(demo)} />}
      <Button
        type="primary"
        htmlType="submit"
        size="large"
        block
        loading={loading}
        className="erp-auth__submit"
      >
        {t('auth.signIn')} <ArrowRightOutlined className="erp-auth__arrow" />
      </Button>
      {footer && (
        <Typography.Text type="secondary" className="erp-auth__note">
          {footer}
        </Typography.Text>
      )}
    </Form>
  );
}
