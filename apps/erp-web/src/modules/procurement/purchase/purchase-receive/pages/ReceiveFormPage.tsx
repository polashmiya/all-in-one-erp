import {
  getGetPurchaseReceiveQueryKey,
  useCreatePurchaseReceive,
  useGetPurchaseReceive,
  useUpdatePurchaseReceive,
  type ReceiveOrderOption,
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
import { ReceiveLines } from '../components/ReceiveLines';
import { RECEIVE_FEATURE, receivePaths } from '../constants';
import {
  fromDetail,
  fromOrder,
  newReceive,
  toRequest,
  useReceiveLookups,
  type ReceiveValues,
} from '../receive-form';

export function ReceiveFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(RECEIVE_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(RECEIVE_FEATURE, allowed);
  const draft = useFormDraft<ReceiveValues>(RECEIVE_FEATURE, allowed && !id);
  const source = useGetPurchaseReceive(id, {
    query: { queryKey: useUnitKey(getGetPurchaseReceiveQueryKey(id)), enabled: allowed && id > 0 },
  });
  const { lookups, orders } = useReceiveLookups(allowed);
  const create = useCreatePurchaseReceive();
  const update = useUpdatePurchaseReceive();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('receive.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: receivePaths.view(record.id) }
          : { key: 'back', label: t('receive.backToList'), to: receivePaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<ReceiveValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newReceive(), ...draft.values }
          }
          options={{
            lookups,
            behaviors: { supplierId: { watch: [], state: () => ({ disabled: true }) } },
            widgets: {
              purchaseOrderId: (_, placeholder) => (
                <SourcePicker<ReceiveOrderOption, ReceiveValues>
                  toValues={fromOrder}
                  options={orders}
                  disabled={!!record}
                  current={
                    record
                      ? { id: record.purchaseOrderId, name: record.purchaseOrderName ?? '' }
                      : undefined
                  }
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<ReceiveLines />}
          paths={receivePaths}
          texts={{
            newTitle: t('receive.form.newTitle'),
            editTitle: t('receive.form.editTitle'),
            save: t('receive.form.save'),
            saved: (code) => t('receive.form.saved', { code }),
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
