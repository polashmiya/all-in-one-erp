import type { CustomerOption } from '@erp/api-client';
import {
  getGetSalesQuotationQueryKey,
  useCreateSalesQuotation,
  useGetSalesQuotation,
  useUpdateSalesQuotation,
} from '@erp/api-client';
import { defaultValues } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { SourcePicker } from '@/common/documents/SourcePicker';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { lookupLabel, useUnitKey } from '@/common/master-data';
import { QuotationLines } from '../components/QuotationLines';
import { FORM_COLUMNS, QUOTATION_FEATURE, QUOTATION_LOOKUPS, quotationPaths } from '../constants';
import {
  fromCustomer,
  fromDetail,
  newQuotation,
  quotationBehaviors,
  toRequest,
  useQuotationLookups,
  type QuotationValues,
} from '../quotation-form';

export function QuotationFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(QUOTATION_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(QUOTATION_FEATURE, allowed);
  const draft = useFormDraft<QuotationValues>(QUOTATION_FEATURE, allowed && !id);
  const source = useGetSalesQuotation(id, {
    query: { queryKey: useUnitKey(getGetSalesQuotationQueryKey(id)), enabled: allowed && id > 0 },
  });
  const lookups = useQuotationLookups(allowed);
  const create = useCreateSalesQuotation();
  const update = useUpdateSalesQuotation();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';
  const customers = lookups[QUOTATION_LOOKUPS.customer] as CustomerOption[] | undefined;

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('quotation.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: quotationPaths.view(record.id) }
          : { key: 'back', label: t('quotation.backToList'), to: quotationPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<QuotationValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newQuotation(), ...draft.values }
          }
          options={{
            lookups,
            behaviors: quotationBehaviors,
            maxColumns: FORM_COLUMNS,
            widgets: {
              customerId: (_, placeholder) => (
                <SourcePicker<CustomerOption, QuotationValues>
                  toValues={fromCustomer}
                  labelOf={lookupLabel}
                  options={customers?.filter((customer) => customer.isActive)}
                  current={
                    record ? { id: record.customerId, name: record.customerName } : undefined
                  }
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<QuotationLines />}
          paths={quotationPaths}
          texts={{
            newTitle: t('quotation.form.newTitle'),
            editTitle: t('quotation.form.editTitle'),
            save: t('quotation.form.save'),
            saved: (code) => t('quotation.form.saved', { code }),
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
