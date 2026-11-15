import { CheckCircleFilled, CloseCircleOutlined, LockOutlined } from '@ant-design/icons';
import { Form, Input } from 'antd';
import type { NamePath } from 'antd/es/form/interface';
import { useTranslation } from 'react-i18next';
import { isStrongPassword, PASSWORD_MIN_LENGTH, passwordRules } from './sign-in-rules';

export function PasswordRuleList({ value }: { value: string }) {
  const { t } = useTranslation();
  return (
    <ul className="erp-password-rules">
      {passwordRules.map((rule) => {
        const met = rule.test(value);
        return (
          <li key={rule.key} className={met ? 'is-met' : undefined}>
            {met ? <CheckCircleFilled /> : <CloseCircleOutlined />}
            {t(rule.key, { count: PASSWORD_MIN_LENGTH })}
          </li>
        );
      })}
    </ul>
  );
}

export function NewPasswordFields({
  name = 'newPassword',
  confirmName = 'confirmPassword',
  label,
  size,
}: {
  name?: NamePath;
  confirmName?: NamePath;
  label?: string;
  size?: 'large';
}) {
  const { t } = useTranslation();
  const form = Form.useFormInstance();
  const value = (Form.useWatch(name, form) as string | undefined) ?? '';

  return (
    <>
      <Form.Item
        name={name}
        label={label ?? t('auth.newPassword')}
        rules={[
          { required: true, message: t('auth.newPasswordRequired') },
          {
            validator: (_, next: string | undefined) =>
              !next || isStrongPassword(next)
                ? Promise.resolve()
                : Promise.reject(new Error(t('auth.passwordWeak'))),
          },
        ]}
        extra={<PasswordRuleList value={value} />}
      >
        <Input.Password size={size} prefix={<LockOutlined />} autoComplete="new-password" />
      </Form.Item>
      <Form.Item
        name={confirmName}
        label={t('auth.confirmPassword')}
        dependencies={[name]}
        rules={[
          { required: true, message: t('auth.passwordMismatch') },
          ({ getFieldValue }) => ({
            validator: (_, next: string | undefined) =>
              !next || next === getFieldValue(name)
                ? Promise.resolve()
                : Promise.reject(new Error(t('auth.passwordMismatch'))),
          }),
        ]}
      >
        <Input.Password size={size} prefix={<LockOutlined />} autoComplete="new-password" />
      </Form.Item>
    </>
  );
}
