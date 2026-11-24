import type { OrderRequisitionOption } from '@erp/api-client';
import {
  getGetPurchaseOrderQueryKey,
  useCreatePurchaseOrder,
  useGetPurchaseOrder,
  useUpdatePurchaseOrder,
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
import { OrderLines } from '../components/OrderLines';
import { SourcePicker } from '../../../components/SourcePicker';
import { ORDER_FEATURE, ORDER_LOOKUPS, orderPaths } from '../constants';
import {
  fromDetail,
  fromRequisition,
  newOrder,
  toRequest,
  useOrderLookups,
  type OrderValues,
} from '../order-form';

export function OrderFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(ORDER_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(ORDER_FEATURE, allowed);
  const draft = useFormDraft<OrderValues>(ORDER_FEATURE, allowed && !id);
  const source = useGetPurchaseOrder(id, {
    query: { queryKey: useUnitKey(getGetPurchaseOrderQueryKey(id)), enabled: allowed && id > 0 },
  });
  const lookups = useOrderLookups(allowed);
  const create = useCreatePurchaseOrder();
  const update = useUpdatePurchaseOrder();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';
  const requisitions = lookups[ORDER_LOOKUPS.requisition] as OrderRequisitionOption[] | undefined;

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('order.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: orderPaths.view(record.id) }
          : { key: 'back', label: t('order.backToList'), to: orderPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<OrderValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newOrder(), ...draft.values }
          }
          options={{
            lookups,
            widgets: {
              requisitionId: (_, placeholder) => (
                <SourcePicker<OrderRequisitionOption, OrderValues>
                  allowClear
                  toValues={fromRequisition}
                  options={requisitions}
                  current={
                    record?.requisitionId
                      ? { id: record.requisitionId, name: record.requisitionName ?? '' }
                      : undefined
                  }
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<OrderLines />}
          paths={orderPaths}
          texts={{
            newTitle: t('order.form.newTitle'),
            editTitle: t('order.form.editTitle'),
            save: t('order.form.save'),
            saved: (code) => t('order.form.saved', { code }),
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
