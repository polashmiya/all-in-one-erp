import {
  getGetSupplierQueryKey,
  useCreateSupplier,
  useGetSupplier,
  useUpdateSupplier,
  type SaveSupplierRequest,
} from '@erp/api-client';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { PartnerForm } from '../../components/PartnerForm';
import { SUPPLIER_FEATURE, supplierPaths } from '../../partner-management';
import { toRequest, useSupplierLookups } from '../supplier-form';

export function SupplierFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(SUPPLIER_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(SUPPLIER_FEATURE, allowed);
  const draft = useFormDraft<SaveSupplierRequest>(SUPPLIER_FEATURE, allowed && !id);
  const source = useGetSupplier(id, {
    query: { queryKey: useUnitKey(getGetSupplierQueryKey(id)), enabled: allowed && id > 0 },
  });
  const lookups = useSupplierLookups(allowed);
  const create = useCreateSupplier();
  const update = useUpdateSupplier();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !source.data) || !config.data)}
      missingTitle={t('supplier.notFound')}
      missingActions={[{ key: 'back', label: t('supplier.backToList'), to: supplierPaths.list }]}
    >
      {config.data && (
        <PartnerForm<SaveSupplierRequest>
          key={id || 'new'}
          config={config.data}
          record={id ? source.data : undefined}
          draft={draft}
          lookups={lookups}
          paths={supplierPaths}
          texts={{
            newTitle: t('supplier.form.newTitle'),
            editTitle: t('supplier.form.editTitle'),
            save: t('supplier.form.save'),
            saved: (code) => t('supplier.form.saved', { code }),
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
