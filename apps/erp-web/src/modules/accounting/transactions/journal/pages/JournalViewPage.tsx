import { BookOutlined, CalendarOutlined, FileDoneOutlined } from '@ant-design/icons';
import { getGetJournalQueryKey, useGetJournal, type JournalLineDetail } from '@erp/api-client';
import {
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  TitleCell,
} from '@erp/ui';
import { Table } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router';
import '@/common/documents/documents.css';
import { DocumentView } from '@/common/documents/DocumentView';
import { LineTotals } from '@/common/documents/LineTotals';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { documentLink } from '@/modules/document-registry';
import { JOURNAL_FEATURE, journalPaths } from '../constants';
import { journalCalls } from '../journal-form';

export function JournalViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(JOURNAL_FEATURE);
  const config = useFeatureConfig(JOURNAL_FEATURE, access.view);
  const query = useGetJournal(id, {
    query: { queryKey: useUnitKey(getGetJournalQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const journal = query.data;
  if (!journal || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('journal.notFound')}
        missingActions={[{ key: 'back', label: t('journal.backToList'), to: journalPaths.list }]}
      />
    );
  }

  const source = documentLink(journal.feature);
  const money = (value: number) => (value ? formatMoney(value) : '—');

  return (
    <DocumentView
      document={journal}
      config={config.data}
      feature={JOURNAL_FEATURE}
      paths={journalPaths}
      calls={journalCalls}
      icon={<BookOutlined />}
      tag={t(journal.isManual ? 'journal.manual' : 'journal.automatic')}
      meta={[
        { key: 'date', icon: <CalendarOutlined />, label: formatDate(journal.journalDate) },
        {
          key: 'source',
          icon: <FileDoneOutlined />,
          label: source && journal.documentId && (
            <Link to={source.path(journal.documentId)}>
              {t(source.label)} {journal.documentCode}
            </Link>
          ),
        },
      ]}
      lines={
        <DetailSection title={t('journal.lines.title')}>
          <div className="erp-lines">
            <Table<JournalLineDetail>
              size="small"
              rowKey="id"
              pagination={false}
              dataSource={journal.lines}
              scroll={{ x: 'max-content' }}
              columns={[
                {
                  key: 'account',
                  title: t('accounting.account'),
                  render: (_, line) => (
                    <TitleCell title={line.accountName} subtitle={line.accountCode} />
                  ),
                },
                {
                  key: 'supplier',
                  title: t('journal.lines.supplier'),
                  render: (_, line) => line.supplierName ?? '—',
                },
                {
                  key: 'customer',
                  title: t('journal.lines.customer'),
                  render: (_, line) => line.customerName ?? '—',
                },
                {
                  key: 'remarks',
                  title: t('journal.lines.remarks'),
                  render: (_, line) => line.remarks ?? '—',
                },
                {
                  key: 'debit',
                  title: t('accounting.debit'),
                  align: 'end',
                  render: (_, line) => money(line.debit),
                },
                {
                  key: 'credit',
                  title: t('accounting.credit'),
                  align: 'end',
                  render: (_, line) => money(line.credit),
                },
              ]}
            />
            <div className="erp-lines__footer">
              <span />
              <LineTotals
                items={[
                  {
                    key: 'debit',
                    label: t('journal.totals.debit'),
                    value: formatMoney(journal.totalAmount),
                  },
                  {
                    key: 'credit',
                    label: t('journal.totals.credit'),
                    value: formatMoney(journal.totalAmount),
                  },
                ]}
              />
            </div>
          </div>
        </DetailSection>
      }
    />
  );
}
