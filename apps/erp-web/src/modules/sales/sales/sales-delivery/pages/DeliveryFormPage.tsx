import type { CustomerOption } from '@erp/api-client';
import {
  getGetSalesDeliveryQueryKey,
  useCreateSalesDelivery,
  useGetSalesDelivery,
  useUpdateSalesDelivery,
} from '@erp/api-client';
import { defaultValues } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { FORM_COLUMNS } from '../../../sales-document';
import { DeliveryLines } from '../components/DeliveryLines';
import { DeliveryCustomerPicker, OrderPicker } from '../components/DeliveryPickers';
import { DeliveryTypeSelect } from '../components/DeliveryTypeSelect';
import { DELIVERY_FEATURE, DELIVERY_LOOKUPS, deliveryPaths, ORDER_PARAM } from '../constants';
import {
  deliveryBehaviors,
  fromDetail,
  fromOrder,
  newDelivery,
  toRequest,
  useDeliveryLookups,
  type DeliveryValues,
} from '../delivery-form';

export function DeliveryFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const [search] = useSearchParams();
  const orderId = id ? 0 : Number(search.get(ORDER_PARAM) ?? 0);
  const access = useFeatureAccess(DELIVERY_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(DELIVERY_FEATURE, allowed);
  const draft = useFormDraft<DeliveryValues>(DELIVERY_FEATURE, allowed && !id);
  const source = useGetSalesDelivery(id, {
    query: { queryKey: useUnitKey(getGetSalesDeliveryQueryKey(id)), enabled: allowed && id > 0 },
  });
  const { lookups, payFrom, orders } = useDeliveryLookups(allowed);
  const create = useCreateSalesDelivery();
  const update = useUpdateSalesDelivery();

  if (!allowed) return <ForbiddenResult />;

  const loading =
    (id > 0 && source.isPending) || config.isPending || draft.loading || (orderId > 0 && !orders);
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';
  const customers = lookups[DELIVERY_LOOKUPS.customer] as CustomerOption[] | undefined;
  const order = orders?.find((o) => o.id === orderId);

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('salesDelivery.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: deliveryPaths.view(record.id) }
          : { key: 'back', label: t('salesDelivery.backToList'), to: deliveryPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<DeliveryValues>
          key={id || `new-${orderId}`}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : {
                  ...defaultValues(config.data),
                  ...newDelivery(),
                  ...(order && fromOrder(order)),
                  ...draft.values,
                }
          }
          options={{
            lookups,
            behaviors: deliveryBehaviors(payFrom.behaviors),
            maxColumns: FORM_COLUMNS,
            widgets: {
              type: (_, placeholder) => (
                <DeliveryTypeSelect
                  types={lookups[DELIVERY_LOOKUPS.type] ?? []}
                  placeholder={placeholder}
                />
              ),
              orderId: (_, placeholder) => (
                <OrderPicker
                  orders={orders}
                  current={
                    record?.orderId
                      ? { id: record.orderId, name: record.orderName ?? '' }
                      : undefined
                  }
                  placeholder={placeholder}
                />
              ),
              customerId: (_, placeholder) => (
                <DeliveryCustomerPicker
                  customers={customers}
                  current={
                    record ? { id: record.customerId, name: record.customerName } : undefined
                  }
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<DeliveryLines payFrom={payFrom} />}
          paths={deliveryPaths}
          texts={{
            newTitle: t('salesDelivery.form.newTitle'),
            editTitle: t('salesDelivery.form.editTitle'),
            save: t('salesDelivery.form.save'),
            saved: (code) => t('salesDelivery.form.saved', { code }),
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
