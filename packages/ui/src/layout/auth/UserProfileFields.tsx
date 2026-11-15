import { MailOutlined, PhoneOutlined, UserOutlined } from '@ant-design/icons';
import { Col, Form, Input, Row } from 'antd';
import type { NamePath } from 'antd/es/form/interface';
import { useTranslation } from 'react-i18next';
import { usePlaceholder } from '../../components/fields/placeholder';
import { EMAIL_MAX_LENGTH, FULL_NAME_MAX_LENGTH, PHONE_MAX_LENGTH } from './sign-in-rules';
import { useUsernameRules } from './useUsernameRules';

export interface UserProfileValues {
  fullName: string;
  username: string;
  email: string;
  phone?: string | null;
}

export function UsernameField({ name = 'username' }: { name?: NamePath }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  return (
    <Form.Item
      name={name}
      label={t('userProfile.username')}
      extra={t('userProfile.usernameHelp')}
      rules={useUsernameRules()}
    >
      <Input
        prefix={<UserOutlined />}
        placeholder={placeholder('enter', t('userProfile.username'))}
        autoComplete="off"
      />
    </Form.Item>
  );
}

export function UserProfileFields() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();

  return (
    <Row gutter={16}>
      <Col xs={24} md={12}>
        <Form.Item
          name="fullName"
          label={t('userProfile.fullName')}
          rules={[{ required: true, whitespace: true }, { max: FULL_NAME_MAX_LENGTH }]}
        >
          <Input
            prefix={<UserOutlined />}
            placeholder={placeholder('enter', t('userProfile.fullName'))}
          />
        </Form.Item>
      </Col>
      <Col xs={24} md={12}>
        <UsernameField />
      </Col>
      <Col xs={24} md={12}>
        <Form.Item
          name="email"
          label={t('userProfile.email')}
          rules={[{ required: true, type: 'email' }, { max: EMAIL_MAX_LENGTH }]}
        >
          <Input
            prefix={<MailOutlined />}
            placeholder={t('auth.emailPlaceholder')}
            autoComplete="off"
          />
        </Form.Item>
      </Col>
      <Col xs={24} md={12}>
        <Form.Item name="phone" label={t('userProfile.phone')} rules={[{ max: PHONE_MAX_LENGTH }]}>
          <Input
            prefix={<PhoneOutlined />}
            placeholder={placeholder('enter', t('userProfile.phone'))}
          />
        </Form.Item>
      </Col>
    </Row>
  );
}
