import { CheckCircleFilled, UserOutlined } from '@ant-design/icons';
import { Alert, Button, Form, Input } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NewPasswordFields } from './PasswordFields';
import { AuthHeading } from './SignInForm';

const CODE_LENGTH = 6;

export interface ResetPasswordValues {
  login: string;
  code: string;
  newPassword: string;
}

export interface ForgotPasswordFlowProps {
  initialLogin?: string;
  allowUsername?: boolean;
  onSendCode: (login: string) => Promise<void>;
  onReset: (values: ResetPasswordValues) => Promise<string | null>;
  onBack: () => void;
  onDone: () => void;
}

type Step = 'login' | 'code' | 'done';

export function ForgotPasswordFlow({
  initialLogin,
  allowUsername = false,
  onSendCode,
  onReset,
  onBack,
  onDone,
}: ForgotPasswordFlowProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState<Step>('login');
  const [login, setLogin] = useState(initialLogin ?? '');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [codeForm] = Form.useForm<ResetPasswordValues & { confirmPassword: string }>();

  const run = async (action: () => Promise<void>) => {
    setError(null);
    setLoading(true);
    try {
      await action();
    } catch {
      setError(t('auth.resetFailed'));
    } finally {
      setLoading(false);
    }
  };

  const sendCode = (value: string) =>
    run(async () => {
      await onSendCode(value);
      setLogin(value);
      setStep('code');
    });

  const reset = (values: ResetPasswordValues) =>
    run(async () => {
      const codeError = await onReset({
        login,
        code: values.code,
        newPassword: values.newPassword,
      });
      if (codeError) codeForm.setFields([{ name: 'code', errors: [codeError] }]);
      else setStep('done');
    });

  if (step === 'done') {
    return (
      <div className="erp-auth__done">
        <CheckCircleFilled className="erp-auth__done-icon" />
        <AuthHeading title={t('auth.resetDoneTitle')} lead={t('auth.resetDoneLead')} />
        <Button type="primary" size="large" block onClick={onDone} className="erp-auth__submit">
          {t('auth.backToSignIn')}
        </Button>
      </div>
    );
  }

  return (
    <>
      <AuthHeading
        title={step === 'login' ? t('auth.forgotTitle') : t('auth.checkEmailTitle')}
        lead={step === 'login' ? t('auth.forgotLead') : t('auth.checkEmailLead', { login })}
      />
      {error && <Alert type="error" showIcon message={error} className="erp-auth__alert" />}
      {step === 'login' ? (
        <Form<{ login: string }>
          key="login"
          layout="vertical"
          requiredMark={false}
          initialValues={{ login }}
          onFinish={(values) => void sendCode(values.login.trim())}
          className="erp-form erp-auth__form"
          disabled={loading}
        >
          <Form.Item
            name="login"
            label={allowUsername ? t('auth.login') : t('auth.email')}
            rules={[{ required: true, whitespace: true, message: t('auth.loginRequired') }]}
          >
            <Input
              size="large"
              prefix={<UserOutlined />}
              placeholder={allowUsername ? t('auth.loginPlaceholder') : t('auth.emailPlaceholder')}
              autoFocus
            />
          </Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            size="large"
            block
            loading={loading}
            className="erp-auth__submit"
          >
            {t('auth.sendCode')}
          </Button>
          <Button type="link" block onClick={onBack}>
            {t('common.back')}
          </Button>
        </Form>
      ) : (
        <Form<ResetPasswordValues & { confirmPassword: string }>
          key="code"
          form={codeForm}
          layout="vertical"
          requiredMark={false}
          onFinish={(values) => void reset(values)}
          className="erp-form erp-auth__form"
          disabled={loading}
        >
          <Form.Item
            name="code"
            label={t('auth.code')}
            extra={t('auth.codeHelp')}
            rules={[{ required: true, len: CODE_LENGTH, message: t('auth.codeRequired') }]}
          >
            <Input.OTP length={CODE_LENGTH} size="large" />
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
            {t('auth.resetSubmit')}
          </Button>
          <div className="erp-auth__links">
            <Button type="link" onClick={() => void sendCode(login)}>
              {t('auth.sendAgain')}
            </Button>
            <Button type="link" onClick={() => setStep('login')}>
              {t('auth.useAnotherLogin')}
            </Button>
          </div>
        </Form>
      )}
    </>
  );
}
