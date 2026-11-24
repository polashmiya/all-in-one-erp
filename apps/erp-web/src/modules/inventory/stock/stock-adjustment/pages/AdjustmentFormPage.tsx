import {
  getGetStockAdjustmentQueryKey,
  useCreateStockAdjustment,
  useGetStockAdjustment,
  useUpdateStockAdjustment,
} from '@erp/api-client';
import { defaultValues } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { AdjustmentLines } from '../components/AdjustmentLines';
import {
  fromDetail,
  newAdjustment,
  toRequest,
  useAdjustmentLookups,
  type AdjustmentValues,
} from '../adjustment-form';
import { ADJUSTMENT_FEATURE, adjustmentPaths } from '../constants';

export function AdjustmentFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(ADJUSTMENT_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(ADJUSTMENT_FEATURE, allowed);
  const draft = useFormDraft<AdjustmentValues>(ADJUSTMENT_FEATURE, allowed && !id);
  const source = useGetStockAdjustment(id, {
    query: {
      queryKey: useUnitKey(getGetStockAdjustmentQueryKey(id)),
      enabled: allowed && id > 0,
    },
  });
  const lookups = useAdjustmentLookups(allowed);
  const create = useCreateStockAdjustment();
  const update = useUpdateStockAdjustment();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('adjustment.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: adjustmentPaths.view(record.id) }
          : { key: 'back', label: t('adjustment.backToList'), to: adjustmentPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<AdjustmentValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newAdjustment(), ...draft.values }
          }
          options={{ lookups }}
          lines={<AdjustmentLines />}
          paths={adjustmentPaths}
          texts={{
            newTitle: t('adjustment.form.newTitle'),
            editTitle: t('adjustment.form.editTitle'),
            save: t('adjustment.form.save'),
            saved: (code) => t('adjustment.form.saved', { code }),
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
