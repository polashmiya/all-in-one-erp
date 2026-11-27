import { SaveOutlined } from '@ant-design/icons';
import {
  applyFieldErrors,
  getListPostingAccountsQueryKey,
  problemMessage,
  useListPostingAccounts,
  useSavePostingAccounts,
} from '@erp/api-client';
import { selectOptions } from '@erp/metadata-engine';
import {
  Field,
  FieldSection,
  ForbiddenResult,
  FormPageLayout,
  PageState,
  usePlaceholder,
} from '@erp/ui';
import { App, Form, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { useRefreshDocuments } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useUnitKey } from '@/common/master-data';
import { useAccountOptions } from '../../../accounting';
import { POSTING_FEATURE, POSTING_SECTIONS } from '../constants';

type Values = Record<string, number | null>;

export function PostingAccountsPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const placeholder = usePlaceholder();
  const [form] = Form.useForm<Values>();
  const access = useFeatureAccess(POSTING_FEATURE);
  const accounts = useAccountOptions(access.view);
  const refresh = useRefreshDocuments();
  const query = useListPostingAccounts({
    query: { queryKey: useUnitKey(getListPostingAccountsQueryKey()), enabled: access.view },
  });
  const save = useSavePostingAccounts();

  if (!access.view) return <ForbiddenResult />;

  const submit = async (values: Values) => {
    try {
      const items = Object.entries(values).map(([key, chartOfAccountId]) => ({
        key,
        chartOfAccountId: chartOfAccountId ?? null,
      }));
      await save.mutateAsync({ data: { items } });
      await refresh();
      void message.success(t('posting.saved'));
    } catch (error) {
      void message.error(
        applyFieldErrors(form, error)
          ? t('master.fixFields')
          : problemMessage(error, t('master.failed')),
      );
      throw error;
    }
  };

  return (
    <PageState loading={query.isPending} missing={query.isError}>
      <FormPageLayout<Values>
        form={form}
        initialValues={Object.fromEntries(
          (query.data ?? []).map((row) => [row.key, row.chartOfAccountId]),
        )}
        title={t('posting.title')}
        subTitle={t('posting.subtitle')}
        saving={save.isPending}
        onSubmit={submit}
        submitActions={[
          {
            key: 'save',
            label: t('posting.save'),
            icon: <SaveOutlined />,
            variant: 'primary',
            hidden: !access.edit,
          },
        ]}
        sections={POSTING_SECTIONS.map((section) => ({
          key: section.key,
          fields: section.keys,
          content: (
            <FieldSection title={t(`posting.section.${section.key}`)}>
              {section.keys.map((key) => (
                <Field
                  key={key}
                  name={key}
                  label={t(`posting.key.${key}`)}
                  extra={t(`posting.hint.${key}`)}
                  rules={[{ required: true, message: t('posting.required') }]}
                >
                  <Select<number>
                    showSearch
                    optionFilterProp="label"
                    disabled={!access.edit}
                    placeholder={placeholder('select', t(`posting.key.${key}`))}
                    options={selectOptions(accounts?.filter((a) => !a.isGroup))}
                  />
                </Field>
              ))}
            </FieldSection>
          ),
        }))}
      />
    </PageState>
  );
}
