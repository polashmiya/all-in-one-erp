import { SaveOutlined } from '@ant-design/icons';
import { useGetAccountFeatures, useListAccounts } from '@erp/api-client';
import {
  DetailSection,
  EmptyState,
  FormPageLayout,
  InitialsTile,
  PageContainer,
  PageState,
  TitleCell,
} from '@erp/ui';
import { Form, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { FeatureAccessTree } from '../../components/FeatureAccess';
import { useSaveFeatures } from '../../components/useSaveFeatures';
import { ACCOUNT_PATHS, ACCOUNT_PICKER_SIZE } from '../../constants';

type Values = { featureCodes: string[] };

export function FeaturePermissionPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const accountId = useParams().id ?? '';
  const [form] = Form.useForm<Values>();
  const accounts = useListAccounts({
    status: 'active',
    sort: 'name',
    pageSize: ACCOUNT_PICKER_SIZE,
  });
  const features = useGetAccountFeatures(accountId, {
    query: { enabled: !!accountId, gcTime: 0 },
  });
  const { save, saving } = useSaveFeatures(accountId);

  const page = {
    title: t('featurePermission.title'),
    subTitle: t('featurePermission.subtitle'),
  };
  const picker = (
    <DetailSection
      title={t('featurePermission.account')}
      description={t('featurePermission.accountHint')}
    >
      <Form.Item>
        <Select
          showSearch
          size="large"
          className="acc-picker"
          aria-label={t('featurePermission.account')}
          placeholder={t('featurePermission.accountPlaceholder')}
          loading={accounts.isPending}
          value={accountId || undefined}
          onChange={(id: string) => navigate(ACCOUNT_PATHS.featuresOf(id))}
          optionFilterProp="label"
          options={(accounts.data?.items ?? []).map((account, index) => ({
            value: account.id,
            label: `${account.name} (${account.code})`,
            account,
            index,
          }))}
          optionRender={({ data: { account, index } }) => (
            <TitleCell
              leading={<InitialsTile name={account.name} index={index} />}
              title={account.name}
              subtitle={`${account.code} · ${t('featurePermission.featureCount', { count: account.features })}`}
            />
          )}
        />
      </Form.Item>
    </DetailSection>
  );

  if (!accountId)
    return (
      <PageContainer {...page}>
        <section className="erp-card">
          {picker}
          <EmptyState
            title={t('featurePermission.choose')}
            description={t('featurePermission.chooseText')}
          />
        </section>
      </PageContainer>
    );

  return (
    <PageState loading={features.isPending} missing={!features.isPending && !features.data}>
      <FormPageLayout<Values>
        key={accountId}
        form={form}
        initialValues={{ featureCodes: features.data?.featureCodes }}
        {...page}
        saving={saving}
        onSubmit={(values) => save(values.featureCodes)}
        submitActions={[
          {
            key: 'save',
            label: t('featurePermission.save'),
            icon: <SaveOutlined />,
            variant: 'primary',
          },
        ]}
        sections={[
          { key: 'account', content: picker },
          {
            key: 'features',
            fields: ['featureCodes'],
            content: (
              <DetailSection
                title={t('featurePermission.features')}
                description={t('menus.adminNote')}
              >
                <Form.Item name="featureCodes">
                  <FeatureAccessTree />
                </Form.Item>
              </DetailSection>
            ),
          },
        ]}
      />
    </PageState>
  );
}
