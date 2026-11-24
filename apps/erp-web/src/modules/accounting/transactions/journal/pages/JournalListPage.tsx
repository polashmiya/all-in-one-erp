import {
  getListJournalsQueryKey,
  listJournals,
  useListJournals,
  type JournalRow,
} from '@erp/api-client';
import { useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { DocumentListPage } from '@/common/documents/DocumentListPage';
import { statusParam, type ApprovalView } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, listParams, useUnitKey } from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { documentLink } from '@/modules/document-registry';
import { JOURNAL_FEATURE, journalPaths, MANUAL_SOURCE, POSTING_SOURCES } from '../constants';
import { journalCalls } from '../journal-form';

type Filters = DateRange & { source: string | null };

export function JournalListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(JOURNAL_FEATURE);
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters, ApprovalView>({
    filters: { source: null, ...NO_DATES },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    source: list.filters.source ?? undefined,
    ...dateParams(list.filters),
  };
  const query = useListJournals(params, {
    query: {
      queryKey: useUnitKey(getListJournalsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(JOURNAL_FEATURE, access.view);
  const sourceName = (source: string) =>
    t(source === MANUAL_SOURCE ? 'journal.manual' : (documentLink(source)?.label ?? source));
  const sourceOf = (row: JournalRow) =>
    row.isManual ? t('journal.manual') : `${sourceName(row.feature ?? '')} ${row.documentCode}`;

  return (
    <DocumentListPage<JournalRow, Filters>
      feature={JOURNAL_FEATURE}
      config={config.data}
      title={t('journal.title')}
      subtitle={t('journal.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('journal.new')}
      search={{ label: t('journal.searchLabel'), placeholder: t('journal.search') }}
      list={list}
      filters={[
        {
          key: 'source',
          label: t('journal.col.source'),
          options: [MANUAL_SOURCE, ...POSTING_SOURCES].map((value) => ({
            value,
            label: sourceName(value),
          })),
        },
        ...dates,
      ]}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('accounting.amount'), value: (row) => row.totalAmount }}
      lines={{ label: t('journal.col.source'), value: sourceOf, subtitle: sourceOf }}
      exportFile={t('journal.exportFile')}
      fetchAll={() =>
        listJournals({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('journal.emptyTitle'), description: t('journal.emptyText') }}
      paths={journalPaths}
      calls={journalCalls}
    />
  );
}
