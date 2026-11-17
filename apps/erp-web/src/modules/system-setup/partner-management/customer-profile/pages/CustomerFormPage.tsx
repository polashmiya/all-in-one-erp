import {
  getGetCustomerQueryKey,
  useCreateCustomer,
  useGetCustomer,
  useUpdateCustomer,
  type SaveCustomerRequest,
} from '@erp/api-client';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { PartnerForm } from '../../components/PartnerForm';
import { CUSTOMER_FEATURE, customerPaths } from '../../partner-management';
import { toRequest, useCustomerLookups } from '../customer-form';

export function CustomerFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(CUSTOMER_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(CUSTOMER_FEATURE, allowed);
  const draft = useFormDraft<SaveCustomerRequest>(CUSTOMER_FEATURE, allowed && !id);
  const source = useGetCustomer(id, {
    query: { queryKey: useUnitKey(getGetCustomerQueryKey(id)), enabled: allowed && id > 0 },
  });
  const lookups = useCustomerLookups(allowed);
  const create = useCreateCustomer();
  const update = useUpdateCustomer();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !source.data) || !config.data)}
      missingTitle={t('customer.notFound')}
      missingActions={[{ key: 'back', label: t('customer.backToList'), to: customerPaths.list }]}
    >
      {config.data && (
        <PartnerForm<SaveCustomerRequest>
          key={id || 'new'}
          config={config.data}
          record={id ? source.data : undefined}
          draft={draft}
          lookups={lookups}
          paths={customerPaths}
          texts={{
            newTitle: t('customer.form.newTitle'),
            editTitle: t('customer.form.editTitle'),
            save: t('customer.form.save'),
            saved: (code) => t('customer.form.saved', { code }),
          }}
          saving={create.isPending || update.isPending}
          onSave={(values, recordId) => {
            const data = toRequest(values);
            return recordId
              ? update.mutateAsync({ id: recordId, data })
              : create.mutateAsync({ data });
          }}
        />
      )}
    </PageState>
  );
}
