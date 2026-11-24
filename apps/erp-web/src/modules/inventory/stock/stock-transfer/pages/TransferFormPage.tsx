import {
  getGetStockTransferQueryKey,
  useCreateStockTransfer,
  useGetStockTransfer,
  useUpdateStockTransfer,
  type StructureOption,
} from '@erp/api-client';
import { defaultValues, type LookupItem } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { TransferLines } from '../components/TransferLines';
import { TRANSFER_FEATURE, TRANSFER_LOOKUPS, transferPaths } from '../constants';
import {
  fromDetail,
  newTransfer,
  toRequest,
  useTransferLookups,
  type TransferValues,
} from '../transfer-form';

export function TransferFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(TRANSFER_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(TRANSFER_FEATURE, allowed);
  const draft = useFormDraft<TransferValues>(TRANSFER_FEATURE, allowed && !id);
  const source = useGetStockTransfer(id, {
    query: { queryKey: useUnitKey(getGetStockTransferQueryKey(id)), enabled: allowed && id > 0 },
  });
  const lookups = useTransferLookups(allowed);
  const warehouses = lookups[TRANSFER_LOOKUPS.warehouse];
  const inOffice = (item: LookupItem, officeId: unknown) =>
    !officeId || (item as StructureOption).officeId === officeId;
  const create = useCreateStockTransfer();
  const update = useUpdateStockTransfer();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('transfer.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: transferPaths.view(record.id) }
          : { key: 'back', label: t('transfer.backToList'), to: transferPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<TransferValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newTransfer(), ...draft.values }
          }
          options={{
            lookups,
            behaviors: {
              fromWarehouseId: {
                watch: ['fromOfficeId'],
                state: (values) => ({ filter: (item) => inOffice(item, values.fromOfficeId) }),
              },
              toWarehouseId: {
                watch: ['toOfficeId', 'fromWarehouseId'],
                state: (values) => ({
                  filter: (item) =>
                    item.id !== values.fromWarehouseId && inOffice(item, values.toOfficeId),
                }),
              },
            },
          }}
          lines={<TransferLines warehouses={warehouses as StructureOption[] | undefined} />}
          paths={transferPaths}
          texts={{
            newTitle: t('transfer.form.newTitle'),
            editTitle: t('transfer.form.editTitle'),
            save: t('transfer.form.save'),
            saved: (code) => t('transfer.form.saved', { code }),
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
