import { LockOutlined, MailOutlined, SafetyCertificateOutlined, UserOutlined } from '@ant-design/icons';
import { useLanguageStore } from '@erp/ui';
import { Alert, Button, Card, Col, Descriptions, Divider, Form, Input, Radio, Row } from 'antd';
import { useTranslation } from 'react-i18next';
import { FeatureAccessSummary } from '../../../components/FeatureAccess';
import { structureFromForm } from '../../../components/structure';
import { StructureView } from '../../../components/StructureView';
import { countryName } from '../../../utils/format';
import { FormSection } from '../FormSection';
import { setupRules } from '../../model/rules';
import type { SetupFormValues } from '../../model/setup-model';

export function ReviewStep({ onEdit }: { onEdit: (step: number) => void }) {
  const { t } = useTranslation();
  const language = useLanguageStore((s) => s.language);
  const form = Form.useFormInstance<SetupFormValues>();
  const values = (Form.useWatch([], { form, preserve: true }) as SetupFormValues | undefined) ?? {};
  const passwordMode = Form.useWatch(['admin', 'passwordMode'], form);
  const rules = setupRules(t);
  const account = values.account ?? {};

  const edit = (step: number) => (
    <Button type="link" size="small" onClick={() => onEdit(step)}>
      {t('setup.edit')}
    </Button>
  );

  return (
    <>
      <FormSection title={t('setup.section.admin')}>
        <Alert type="info" showIcon icon={<SafetyCertificateOutlined />} message={t('setup.adminIntro')} className="acc-alert" />
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item name={['admin', 'fullName']} label={t('field.fullName')} rules={[rules.required, { max: 200 }]}>
              <Input prefix={<UserOutlined />} />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              name={['admin', 'email']}
              label={t('field.loginEmail')}
              tooltip={t('setup.loginEmailHelp')}
              validateDebounce={400}
              hasFeedback
              rules={[rules.required, rules.email, { max: 256 }, rules.adminEmailAvailable]}
            >
              <Input prefix={<MailOutlined />} autoComplete="off" />
            </Form.Item>
          </Col>
          <Col xs={24}>
            <Form.Item name={['admin', 'passwordMode']} label={t('field.password')}>
              <Radio.Group
                optionType="button"
                buttonStyle="solid"
                options={[
                  { value: 'generate', label: t('setup.passwordGenerate') },
                  { value: 'manual', label: t('setup.passwordManual') },
                ]}
              />
            </Form.Item>
          </Col>
          {passwordMode === 'manual' && (
            <>
              <Col xs={24} md={12}>
                <Form.Item name={['admin', 'password']} label={t('field.password')} extra={t('setup.passwordRule')} rules={[{ required: true }, rules.strongPassword]}>
                  <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item
                  name={['admin', 'confirmPassword']}
                  label={t('field.confirmPassword')}
                  dependencies={[['admin', 'password']]}
                  rules={[
                    { required: true },
                    ({ getFieldValue }) => ({
                      validator: async (_, value?: string) => {
                        if (value && value !== getFieldValue(['admin', 'password'])) throw new Error(t('setup.passwordMismatch'));
                      },
                    }),
                  ]}
                >
                  <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
                </Form.Item>
              </Col>
            </>
          )}
        </Row>
      </FormSection>

      <Divider />

      <FormSection title={t('setup.review')} description={t('setup.reviewHint')}>
        <Descriptions
          bordered
          size="small"
          column={{ xs: 1, sm: 1, md: 2, lg: 2, xl: 2, xxl: 2 }}
          styles={{ label: { width: 140 } }}
          title={t('setup.step.account')}
          extra={edit(0)}
          items={[
            { key: 'name', label: t('field.name'), children: account.name || '—' },
            { key: 'code', label: t('field.code'), children: account.code || '—' },
            { key: 'email', label: t('field.email'), children: account.email || '—' },
            { key: 'phone', label: t('field.phone'), children: account.phone || '—' },
            { key: 'address', label: t('field.address'), span: 'filled', children: account.address || '—' },
            {
              key: 'location',
              label: t('field.cityCountry'),
              children: [account.city, countryName(account.country, language)].filter(Boolean).join(', ') || '—',
            },
            { key: 'regional', label: t('setup.section.regional'), children: `${account.baseCurrency ?? '—'} · ${account.timeZone ?? '—'}` },
          ]}
        />
        <Card size="small" title={t('detail.structure')} extra={edit(1)} className="acc-card-gap">
          <StructureView units={structureFromForm(values.businessUnits)} />
        </Card>
        <Card size="small" title={t('menus.title')} extra={edit(4)} className="acc-card-gap">
          <FeatureAccessSummary codes={values.featureCodes ?? []} />
        </Card>
      </FormSection>
    </>
  );
}
