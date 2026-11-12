import { CheckOutlined } from '@ant-design/icons';
import {
  ApiError,
  getGetAccountQueryKey,
  useGetAccount,
  useUpdateAccount,
  type AccountDetail,
  type UpdateAccountRequest,
} from '@erp/api-client';
import { Field, FormPageLayout, PageState, YesNoSwitch } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { App, Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { FormSection } from '../../account-setup/components/FormSection';
import { AccountStep } from '../../account-setup/components/steps/AccountStep';
import { ACCOUNT_PATHS } from '../../constants';

export function AccountEditPage() {
  const { t } = useTranslation();
  const id = useParams().id ?? '';
  const query = useGetAccount(id);

  return (
    <PageState
      loading={query.isPending}
      missing={!query.isPending && !query.data}
      missingTitle={t('detail.loadFailed')}
      missingActions={[{ key: 'back', label: t('accounts.title'), to: ACCOUNT_PATHS.list }]}
    >
      {query.data && <AccountEditForm account={query.data} />}
    </PageState>
  );
}

function AccountEditForm({ account }: { account: AccountDetail }) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm<UpdateAccountRequest>();
  const update = useUpdateAccount();
  const detailPath = ACCOUNT_PATHS.detail(account.id);

  const initialValues = useMemo<UpdateAccountRequest>(
    () => ({
      account: {
        code: account.code,
        name: account.name,
        email: account.email ?? '',
        phone: account.phone ?? '',
        website: account.website,
        industry: account.industry,
        taxId: account.taxId,
        address: account.address ?? '',
        city: account.city,
        country: account.country ?? '',
        baseCurrency: account.baseCurrency,
        timeZone: account.timeZone,
      },
      isActive: account.isActive,
    }),
    [account],
  );

  const submit = async (values: UpdateAccountRequest) => {
    try {
      await update.mutateAsync({
        id: account.id,
        data: { ...values, account: { ...values.account, code: account.code } },
      });
      await queryClient.invalidateQueries({ queryKey: getGetAccountQueryKey(account.id) });
      void message.success(t('edit.saved'));
      void navigate(detailPath);
    } catch (error) {
      if (error instanceof ApiError && error.status === 400)
        form.setFields(
          Object.entries(error.fieldErrors).map(([name, errors]) => ({
            name: name.split('.') as never,
            errors,
          })),
        );
      void message.error(t('edit.failed'));
      throw error;
    }
  };

  return (
    <FormPageLayout<UpdateAccountRequest>
      form={form}
      initialValues={initialValues}
      title={t('edit.title', { account: account.name })}
      backTo={detailPath}
      cancelTo={detailPath}
      crumbs={[account.name, t('detail.edit')]}
      saving={update.isPending}
      onSubmit={submit}
      submitActions={[
        { key: 'save', label: t('edit.save'), icon: <CheckOutlined />, variant: 'primary' },
      ]}
    >
      <div className="acc-form">
        <AccountStep lockedCode />
        <FormSection title={t('edit.status')} description={t('edit.statusHint')}>
          <Field name="isActive" label={t('accounts.status.active')} valuePropName="checked">
            <YesNoSwitch />
          </Field>
        </FormSection>
      </div>
    </FormPageLayout>
  );
}
