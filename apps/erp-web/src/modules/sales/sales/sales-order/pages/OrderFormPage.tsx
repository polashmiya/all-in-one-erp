import type { CustomerOption, OrderQuotationOption } from '@erp/api-client';
import {
  getGetSalesOrderQueryKey,
  useCreateSalesOrder,
  useGetSalesOrder,
  useUpdateSalesOrder,
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
import { CustomerPicker } from '../../../components/CustomerPicker';
import { fromCustomer } from '../../../sales-document';
import { OrderLines } from '../components/OrderLines';
import { ORDER_FEATURE, ORDER_LOOKUPS, orderPaths, QUOTATION_PARAM } from '../constants';
import {
  fromDetail,
  fromQuotation,
  newOrder,
  orderBehaviors,
  toRequest,
  useOrderLookups,
  type OrderValues,
} from '../order-form';

export function OrderFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const [search] = useSearchParams();
  const quotationId = id ? 0 : Number(search.get(QUOTATION_PARAM) ?? 0);
  const access = useFeatureAccess(ORDER_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(ORDER_FEATURE, allowed);
  const draft = useFormDraft<OrderValues>(ORDER_FEATURE, allowed && !id);
  const source = useGetSalesOrder(id, {
    query: { queryKey: useUnitKey(getGetSalesOrderQueryKey(id)), enabled: allowed && id > 0 },
  });
  const { lookups, payFrom, quotations } = useOrderLookups(allowed);
  const create = useCreateSalesOrder();
  const update = useUpdateSalesOrder();

  if (!allowed) return <ForbiddenResult />;

  const loading =
    (id > 0 && source.isPending) ||
    config.isPending ||
    draft.loading ||
    (quotationId > 0 && !quotations);
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';
  const customers = lookups[ORDER_LOOKUPS.customer] as CustomerOption[] | undefined;
  const quotation = quotations?.find((q) => q.id === quotationId);

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('salesOrder.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: orderPaths.view(record.id) }
          : { key: 'back', label: t('salesOrder.backToList'), to: orderPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<OrderValues>
          key={id || `new-${quotationId}`}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : {
                  ...defaultValues(config.data),
                  ...newOrder(),
                  ...(quotation && fromQuotation(quotation)),
                  ...draft.values,
                }
          }
          options={{
            lookups,
            behaviors: orderBehaviors(payFrom.behaviors),
            widgets: {
              salesQuotationId: (_, placeholder) => (
                <SourcePicker<OrderQuotationOption, OrderValues>
                  allowClear
                  toValues={fromQuotation}
                  labelOf={(option) => `${option.name} · ${option.customerName}`}
                  options={quotations}
                  current={
                    record?.salesQuotationId
                      ? { id: record.salesQuotationId, name: record.salesQuotationName ?? '' }
                      : undefined
                  }
                  placeholder={placeholder}
                />
              ),
              customerId: (_, placeholder) => (
                <CustomerPicker<OrderValues>
                  toValues={fromCustomer}
                  customers={customers}
                  current={
                    record ? { id: record.customerId, name: record.customerName } : undefined
                  }
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<OrderLines payFrom={payFrom} />}
          paths={orderPaths}
          texts={{
            newTitle: t('salesOrder.form.newTitle'),
            editTitle: t('salesOrder.form.editTitle'),
            save: t('salesOrder.form.save'),
            saved: (code) => t('salesOrder.form.saved', { code }),
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
