import { GlobalOutlined, MailOutlined, PhoneOutlined } from '@ant-design/icons';
import { useLanguageStore } from '@erp/ui';
import { AutoComplete, Col, Divider, Form, Input, Row, Select } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { countryName } from '../../../utils/format';
import { COUNTRIES, CURRENCIES, INDUSTRIES, TIME_ZONES } from '../../../utils/options';
import { FormSection } from '../FormSection';
import { setupRules } from '../../model/rules';
import { normalizeCode } from '../../model/setup-model';

export function AccountStep({ lockedCode = false }: { lockedCode?: boolean }) {
  const { t } = useTranslation();
  const language = useLanguageStore((s) => s.language);
  const rules = setupRules(t);

  const countries = useMemo(
    () =>
      COUNTRIES.map((code) => ({ value: code, label: `${countryName(code, language)} (${code})` })).sort((a, b) =>
        a.label.localeCompare(b.label, language),
      ),
    [language],
  );

  return (
    <>
      <FormSection title={t('setup.section.company')} description={t('setup.section.companyHint')}>
        <Row gutter={16}>
          <Col xs={24} md={14}>
            <Form.Item name={['account', 'name']} label={t('field.name')} rules={[rules.required, { max: 200 }]}>
              <Input placeholder={t('setup.placeholder.accountName')} autoFocus />
            </Form.Item>
          </Col>
          <Col xs={24} md={10}>
            <Form.Item
              name={['account', 'code']}
              label={t('field.code')}
              tooltip={t(lockedCode ? 'edit.codeLocked' : 'setup.codeHelp')}
              validateDebounce={400}
              normalize={normalizeCode}
              hasFeedback={!lockedCode}
              rules={lockedCode ? [] : [...rules.code, { min: 2 }, rules.accountCodeAvailable]}
            >
              <Input placeholder="DEMO-PHARMA" className="erp-code" maxLength={32} disabled={lockedCode} />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name={['account', 'industry']} label={t('field.industry')} rules={[{ max: 100 }]}>
              <AutoComplete
                options={INDUSTRIES.map((value) => ({ value }))}
                filterOption={(input, option) => (option?.value ?? '').toLowerCase().includes(input.toLowerCase())}
                placeholder={t('setup.placeholder.industry')}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name={['account', 'taxId']} label={t('field.taxId')} rules={[{ max: 50 }]}>
              <Input placeholder={t('setup.placeholder.taxId')} />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name={['account', 'website']} label={t('field.website')} rules={[rules.website, { max: 200 }]}>
              <Input prefix={<GlobalOutlined />} placeholder="https://" />
            </Form.Item>
          </Col>
        </Row>
      </FormSection>

      <Divider />

      <FormSection title={t('setup.section.contact')} description={t('setup.section.contactHint')}>
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item name={['account', 'email']} label={t('field.email')} rules={[rules.required, rules.email, { max: 256 }]}>
              <Input prefix={<MailOutlined />} placeholder="info@company.com" autoComplete="off" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name={['account', 'phone']} label={t('field.phone')} rules={[rules.required, rules.phone]}>
              <Input prefix={<PhoneOutlined />} placeholder="+8801XXXXXXXXX" />
            </Form.Item>
          </Col>
          <Col xs={24}>
            <Form.Item name={['account', 'address']} label={t('field.address')} rules={[rules.required, { max: 500 }]}>
              <Input.TextArea autoSize={{ minRows: 2, maxRows: 4 }} placeholder={t('setup.placeholder.address')} />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name={['account', 'city']} label={t('field.city')} rules={[{ max: 100 }]}>
              <Input placeholder={t('setup.placeholder.city')} />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name={['account', 'country']} label={t('field.country')} rules={[{ required: true }]}>
              <Select showSearch optionFilterProp="label" options={countries} />
            </Form.Item>
          </Col>
        </Row>
      </FormSection>

      <Divider />

      <FormSection title={t('setup.section.regional')} description={t('setup.section.regionalHint')}>
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item name={['account', 'baseCurrency']} label={t('field.baseCurrency')} rules={[{ required: true }]}>
              <Select showSearch options={CURRENCIES.map((value) => ({ value, label: value }))} />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name={['account', 'timeZone']} label={t('field.timeZone')} rules={[{ required: true }]}>
              <Select showSearch options={TIME_ZONES.map((value) => ({ value, label: value.replace('_', ' ') }))} />
            </Form.Item>
          </Col>
        </Row>
      </FormSection>
    </>
  );
}
