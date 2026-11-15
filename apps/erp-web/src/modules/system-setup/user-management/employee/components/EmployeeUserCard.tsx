import { UserOutlined } from '@ant-design/icons';
import type { EmployeeDetail } from '@erp/api-client';
import { DetailSection, NewPasswordFields, suggestUsername, UsernameField } from '@erp/ui';
import { Form, Switch } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { USER_FEATURE, userPaths } from '../../user-management';
import type { EmployeeFormValues } from '../employee-form';

export function EmployeeUserCard({ employee }: { employee?: EmployeeDetail }) {
  const { t } = useTranslation();
  const form = Form.useFormInstance<EmployeeFormValues>();
  const isUser = Form.useWatch('isUser', form);
  const canCreate = useFeatureAccess(USER_FEATURE).create;

  if (employee?.userId) {
    return (
      <DetailSection title={t('employee.user.title')}>
        <Link to={userPaths.edit(employee.userId)}>
          <UserOutlined /> @{employee.username}
        </Link>
      </DetailSection>
    );
  }

  if (!canCreate) return null;

  const toggle = (on: boolean) => {
    const email = form.getFieldValue('email') as string | undefined;
    if (on && email && !form.getFieldValue(['user', 'username'])) {
      form.setFieldValue(['user', 'username'], suggestUsername(email));
    }
  };

  return (
    <DetailSection title={t('employee.user.title')} description={t('employee.user.hint')}>
      <Form.Item name="isUser" label={t('employee.user.isUser')} valuePropName="checked">
        <Switch onChange={toggle} />
      </Form.Item>
      {isUser && (
        <>
          <UsernameField name={['user', 'username']} />
          <NewPasswordFields
            name={['user', 'password']}
            confirmName={['user', 'confirmPassword']}
            label={t('employee.user.password')}
          />
        </>
      )}
    </DetailSection>
  );
}
