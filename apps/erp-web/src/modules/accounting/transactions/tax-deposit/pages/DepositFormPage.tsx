import {
  getGetTaxDepositQueryKey,
  useCreateTaxDeposit,
  useGetTaxDeposit,
  useUpdateTaxDeposit,
} from '@erp/api-client';
import { defaultValues } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { DepositLines } from '../components/DepositLines';
import { TaxTypePicker } from '../components/TaxTypePicker';
import { DEPOSIT_FEATURE, depositPaths } from '../constants';
import {
  fromDetail,
  newDeposit,
  toRequest,
  useDepositLookups,
  type DepositValues,
} from '../deposit-form';

export function DepositFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(DEPOSIT_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(DEPOSIT_FEATURE, allowed);
  const draft = useFormDraft<DepositValues>(DEPOSIT_FEATURE, allowed && !id);
  const source = useGetTaxDeposit(id, {
    query: { queryKey: useUnitKey(getGetTaxDepositQueryKey(id)), enabled: allowed && id > 0 },
  });
  const { lookups, types, payFrom } = useDepositLookups(allowed);
  const create = useCreateTaxDeposit();
  const update = useUpdateTaxDeposit();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('deposit.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: depositPaths.view(record.id) }
          : { key: 'back', label: t('deposit.backToList'), to: depositPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<DepositValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newDeposit(), ...draft.values }
          }
          options={{
            lookups,
            behaviors: payFrom.behaviors,
            widgets: {
              type: (_, placeholder) => (
                <TaxTypePicker types={types} depositId={record?.id} placeholder={placeholder} />
              ),
            },
          }}
          lines={<DepositLines payFrom={payFrom} />}
          paths={depositPaths}
          texts={{
            newTitle: t('deposit.form.newTitle'),
            editTitle: t('deposit.form.editTitle'),
            save: t('deposit.form.save'),
            saved: (code) => t('deposit.form.saved', { code }),
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
