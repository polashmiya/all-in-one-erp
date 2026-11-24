import {
  getGetPurchasePaymentQueryKey,
  useCreatePurchasePayment,
  useGetPurchasePayment,
  useUpdatePurchasePayment,
} from '@erp/api-client';
import { defaultValues, type FieldBehavior } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { PaymentLines } from '../components/PaymentLines';
import { SupplierBillsPicker } from '../components/SupplierBillsPicker';
import { BANK_FIELDS, PAYMENT_FEATURE, paymentPaths } from '../constants';
import {
  fromDetail,
  newPayment,
  toRequest,
  usePaymentLookups,
  type PaymentValues,
} from '../payment-form';

export function PaymentFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(PAYMENT_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(PAYMENT_FEATURE, allowed);
  const draft = useFormDraft<PaymentValues>(PAYMENT_FEATURE, allowed && !id);
  const source = useGetPurchasePayment(id, {
    query: { queryKey: useUnitKey(getGetPurchasePaymentQueryKey(id)), enabled: allowed && id > 0 },
  });
  const { lookups, suppliers, payFrom } = usePaymentLookups(allowed);
  const create = useCreatePurchasePayment();
  const update = useUpdatePurchasePayment();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';
  const bankField: FieldBehavior = {
    watch: ['paymentMethodId'],
    state: (values) => ({ hidden: payFrom.isCash(values.paymentMethodId) }),
  };

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('payment.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: paymentPaths.view(record.id) }
          : { key: 'back', label: t('payment.backToList'), to: paymentPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<PaymentValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newPayment(), ...draft.values }
          }
          options={{
            lookups,
            behaviors: {
              ...Object.fromEntries(BANK_FIELDS.map((key) => [key, bankField])),
              ...payFrom.behaviors,
              advanceAmount: {
                watch: ['isAdvance'],
                state: (values) => ({ hidden: !values.isAdvance, required: true }),
              },
            },
            widgets: {
              supplierId: (_, placeholder) => (
                <SupplierBillsPicker
                  suppliers={suppliers}
                  paymentId={record?.id}
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<PaymentLines paymentId={record?.id} payFrom={payFrom} />}
          paths={paymentPaths}
          texts={{
            newTitle: t('payment.form.newTitle'),
            editTitle: t('payment.form.editTitle'),
            save: t('payment.form.save'),
            saved: (code) => t('payment.form.saved', { code }),
          }}
          saving={create.isPending || update.isPending}
          onSave={(values, recordId) => {
            const data = toRequest(values, payFrom.isCash(values.paymentMethodId));
            return recordId
              ? update.mutateAsync({ id: recordId, data })
              : create.mutateAsync({ data });
          }}
        />
      )}
    </PageState>
  );
}
