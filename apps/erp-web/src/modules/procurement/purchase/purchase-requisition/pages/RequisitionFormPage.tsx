import { getGetPurchaseRequisitionQueryKey, useGetPurchaseRequisition } from '@erp/api-client';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { RequisitionForm } from '../components/RequisitionForm';
import { REQUISITION_FEATURE, requisitionPaths } from '../constants';
import type { RequisitionValues } from '../requisition-form';

export function RequisitionFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(REQUISITION_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(REQUISITION_FEATURE, allowed);
  const draft = useFormDraft<RequisitionValues>(REQUISITION_FEATURE, allowed && !id);
  const source = useGetPurchaseRequisition(id, {
    query: {
      queryKey: useUnitKey(getGetPurchaseRequisitionQueryKey(id)),
      enabled: allowed && id > 0,
    },
  });

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('requisition.form.locked') : t('requisition.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: requisitionPaths.view(record.id) }
          : { key: 'back', label: t('requisition.backToList'), to: requisitionPaths.list },
      ]}
    >
      {config.data && (
        <RequisitionForm key={id || 'new'} config={config.data} record={record} draft={draft} />
      )}
    </PageState>
  );
}
