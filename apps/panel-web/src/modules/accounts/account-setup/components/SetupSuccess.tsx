import { SafetyCertificateOutlined } from '@ant-design/icons';
import type { AccountSetupResult } from '@erp/api-client';
import { Alert, Button, Card, Descriptions, Result, Space, Typography } from 'antd';
import { useTranslation } from 'react-i18next';

export function SetupSuccess({
  result,
  onDone,
  onAnother,
}: {
  result: AccountSetupResult;
  onDone: () => void;
  onAnother: () => void;
}) {
  const { t } = useTranslation();
  const password = result.admin.generatedPassword;

  return (
    <Card>
      <Result
        status="success"
        title={t('success.title', { name: result.name })}
        subTitle={t('success.subtitle', {
          code: result.code,
          units: result.businessUnits,
          offices: result.offices,
          warehouses: result.warehouses,
          features: result.features,
        })}
        extra={[
          <Button key="list" type="primary" onClick={onDone}>
            {t('success.toList')}
          </Button>,
          <Button key="again" onClick={onAnother}>
            {t('success.another')}
          </Button>,
        ]}
      >
        <Card
          size="small"
          title={
            <Space>
              <SafetyCertificateOutlined />
              {t('success.credentials')}
            </Space>
          }
          className="acc-credentials"
        >
          <Descriptions
            column={1}
            size="small"
            items={[
              { key: 'name', label: t('field.fullName'), children: result.admin.fullName },
              {
                key: 'username',
                label: t('field.username'),
                children: <Typography.Text copyable>{result.admin.username}</Typography.Text>,
              },
              {
                key: 'email',
                label: t('field.loginEmail'),
                children: <Typography.Text copyable>{result.admin.email}</Typography.Text>,
              },
              {
                key: 'password',
                label: t('field.password'),
                children: password ? (
                  <Typography.Text copyable code strong className="acc-password">
                    {password}
                  </Typography.Text>
                ) : (
                  <Typography.Text type="secondary">{t('success.passwordChosen')}</Typography.Text>
                ),
              },
            ]}
          />
          {password && (
            <Alert
              type="warning"
              showIcon
              message={t('success.passwordOnce')}
              className="acc-alert is-after"
            />
          )}
        </Card>
      </Result>
    </Card>
  );
}
