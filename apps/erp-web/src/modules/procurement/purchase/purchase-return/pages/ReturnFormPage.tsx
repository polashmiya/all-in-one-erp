import {
  getGetPurchaseReturnQueryKey,
  useCreatePurchaseReturn,
  useGetPurchaseReturn,
  useUpdatePurchaseReturn,
  type ReturnReceiveOption,
} from '@erp/api-client';
import { defaultValues } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { SourcePicker } from '../../../components/SourcePicker';
import { ReturnLines } from '../components/ReturnLines';
import { RETURN_FEATURE, returnPaths } from '../constants';
import {
  fromDetail,
  fromReceive,
  newReturn,
  toRequest,
  useReturnLookups,
  type ReturnValues,
} from '../return-form';

export function ReturnFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(RETURN_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(RETURN_FEATURE, allowed);
  const draft = useFormDraft<ReturnValues>(RETURN_FEATURE, allowed && !id);
  const source = useGetPurchaseReturn(id, {
    query: { queryKey: useUnitKey(getGetPurchaseReturnQueryKey(id)), enabled: allowed && id > 0 },
  });
  const { lookups, receives } = useReturnLookups(allowed);
  const create = useCreatePurchaseReturn();
  const update = useUpdatePurchaseReturn();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('return.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: returnPaths.view(record.id) }
          : { key: 'back', label: t('return.backToList'), to: returnPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<ReturnValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newReturn(), ...draft.values }
          }
          options={{
            lookups,
            behaviors: { supplierId: { watch: [], state: () => ({ disabled: true }) } },
            widgets: {
              purchaseReceiveId: (_, placeholder) => (
                <SourcePicker<ReturnReceiveOption, ReturnValues>
                  toValues={fromReceive}
                  options={receives}
                  disabled={!!record}
                  current={
                    record
                      ? { id: record.purchaseReceiveId, name: record.purchaseReceiveName ?? '' }
                      : undefined
                  }
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<ReturnLines />}
          paths={returnPaths}
          texts={{
            newTitle: t('return.form.newTitle'),
            editTitle: t('return.form.editTitle'),
            save: t('return.form.save'),
            saved: (code) => t('return.form.saved', { code }),
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
