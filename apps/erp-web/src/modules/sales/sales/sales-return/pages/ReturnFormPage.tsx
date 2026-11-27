import type { CustomerOption } from '@erp/api-client';
import {
  getGetSalesReturnQueryKey,
  useCreateSalesReturn,
  useGetSalesReturn,
  useUpdateSalesReturn,
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
import { TypeSelect } from '../../../components/TypeSelect';
import { ReturnLines } from '../components/ReturnLines';
import { ReturnCustomerPicker, ReturnDeliveryPicker } from '../components/ReturnPickers';
import { DELIVERY_PARAM, RETURN_FEATURE, RETURN_LOOKUPS, returnPaths } from '../constants';
import {
  fromDelivery,
  fromDetail,
  newReturn,
  ofSource,
  returnBehaviors,
  toRequest,
  useReturnLookups,
  type ReturnValues,
} from '../return-form';

export function ReturnFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const [search] = useSearchParams();
  const deliveryId = id ? 0 : Number(search.get(DELIVERY_PARAM) ?? 0);
  const access = useFeatureAccess(RETURN_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(RETURN_FEATURE, allowed);
  const draft = useFormDraft<ReturnValues>(RETURN_FEATURE, allowed && !id);
  const source = useGetSalesReturn(id, {
    query: { queryKey: useUnitKey(getGetSalesReturnQueryKey(id)), enabled: allowed && id > 0 },
  });
  const { lookups, payFrom, deliveries } = useReturnLookups(allowed);
  const create = useCreateSalesReturn();
  const update = useUpdateSalesReturn();

  if (!allowed) return <ForbiddenResult />;

  const loading =
    (id > 0 && source.isPending) ||
    config.isPending ||
    draft.loading ||
    (deliveryId > 0 && !deliveries);
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';
  const delivery = deliveries?.find((d) => d.id === deliveryId);
  const customers = lookups[RETURN_LOOKUPS.customer] as CustomerOption[] | undefined;

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('salesReturn.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: returnPaths.view(record.id) }
          : { key: 'back', label: t('salesReturn.backToList'), to: returnPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<ReturnValues>
          key={id || `new-${deliveryId}`}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : {
                  ...defaultValues(config.data),
                  ...newReturn(),
                  ...(delivery && fromDelivery(delivery)),
                  ...draft.values,
                }
          }
          options={{
            lookups,
            behaviors: returnBehaviors(payFrom.behaviors),
            widgets: {
              source: (_, placeholder) => (
                <TypeSelect<ReturnValues>
                  types={lookups[RETURN_LOOKUPS.source] ?? []}
                  toValues={ofSource}
                  placeholder={placeholder}
                />
              ),
              deliveryId: (_, placeholder) => (
                <ReturnDeliveryPicker
                  deliveries={deliveries}
                  saved={!!record?.deliveryId}
                  current={
                    record?.deliveryId
                      ? { id: record.deliveryId, name: record.deliveryName ?? '' }
                      : undefined
                  }
                  placeholder={placeholder}
                />
              ),
              customerId: (_, placeholder) => (
                <ReturnCustomerPicker
                  customers={customers}
                  current={
                    record ? { id: record.customerId, name: record.customerName } : undefined
                  }
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<ReturnLines payFrom={payFrom} />}
          paths={returnPaths}
          texts={{
            newTitle: t('salesReturn.form.newTitle'),
            editTitle: t('salesReturn.form.editTitle'),
            save: t('salesReturn.form.save'),
            saved: (code) => t('salesReturn.form.saved', { code }),
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
