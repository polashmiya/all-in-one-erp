import {
  getGetJournalQueryKey,
  useCreateJournal,
  useGetJournal,
  useUpdateJournal,
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
import { JournalLines } from '../components/JournalLines';
import { JOURNAL_FEATURE, journalPaths } from '../constants';
import { fromDetail, newJournal, toRequest, type JournalValues } from '../journal-form';

export function JournalFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(JOURNAL_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(JOURNAL_FEATURE, allowed);
  const draft = useFormDraft<JournalValues>(JOURNAL_FEATURE, allowed && !id);
  const source = useGetJournal(id, {
    query: { queryKey: useUnitKey(getGetJournalQueryKey(id)), enabled: allowed && id > 0 },
  });
  const create = useCreateJournal();
  const update = useUpdateJournal();

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('journal.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: journalPaths.view(record.id) }
          : { key: 'back', label: t('journal.backToList'), to: journalPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<JournalValues>
          key={id || 'new'}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : { ...defaultValues(config.data), ...newJournal(), ...draft.values }
          }
          options={{}}
          lines={<JournalLines />}
          paths={journalPaths}
          texts={{
            newTitle: t('journal.form.newTitle'),
            editTitle: t('journal.form.editTitle'),
            save: t('journal.form.save'),
            saved: (code) => t('journal.form.saved', { code }),
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
