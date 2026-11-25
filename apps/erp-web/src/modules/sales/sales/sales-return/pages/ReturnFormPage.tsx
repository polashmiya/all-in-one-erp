import type { ReturnDeliveryOption } from '@erp/api-client';
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
import { SourcePicker } from '@/common/documents/SourcePicker';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { FORM_COLUMNS } from '../../../sales-document';
import { ReturnLines } from '../components/ReturnLines';
import { DELIVERY_PARAM, RETURN_FEATURE, returnPaths } from '../constants';
import {
  fromDelivery,
  fromDetail,
  newReturn,
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
            maxColumns: FORM_COLUMNS,
            widgets: {
              deliveryId: (_, placeholder) => (
                <SourcePicker<ReturnDeliveryOption, ReturnValues>
                  disabled={!!record}
                  toValues={fromDelivery}
                  labelOf={(option) => `${option.code} · ${option.customerName}`}
                  options={deliveries}
                  current={
                    record ? { id: record.deliveryId, name: record.deliveryName } : undefined
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
