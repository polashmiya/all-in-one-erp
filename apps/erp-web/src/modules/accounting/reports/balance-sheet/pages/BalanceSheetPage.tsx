import { BankOutlined, CreditCardOutlined, PieChartOutlined } from '@ant-design/icons';
import { getGetBalanceSheetQueryKey, useGetBalanceSheet } from '@erp/api-client';
import { formatMoney, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, useUnitKey } from '@/common/master-data';
import { ReportPage } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  totalRow,
  useGroupRows,
  useStatementColumns,
  type StatementRow,
} from '../../../components/statement';
import { accountLedgerLink } from '../../account-ledger/constants';
import { BALANCE_SHEET_FEATURE } from '../constants';

type Filters = { to: string | null };

export function BalanceSheetPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(BALANCE_SHEET_FEATURE);
  const columns = useStatementColumns();
  const groupRows = useGroupRows();
  const list = useListState<Filters>({ filters: { to: null }, pageSize: EXPORT_PAGE_SIZE });

  const params = { to: list.filters.to ?? undefined };
  const query = useGetBalanceSheet(params, {
    query: {
      queryKey: useUnitKey(getGetBalanceSheetQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const data = query.data;
  const rows: StatementRow[] | undefined = data && [
    ...data.assets.flatMap(groupRows),
    totalRow('totalAssets', t('balanceSheet.totalAssets'), data.totalAssets),
    ...data.liabilities.flatMap(groupRows),
    totalRow('totalLiabilities', t('balanceSheet.totalLiabilities'), data.totalLiabilities),
    ...groupRows(data.equity).slice(0, -1),
    { key: 'earnings', name: t('balanceSheet.earnings'), amount: data.currentEarnings },
    totalRow('totalEquity', t('balanceSheet.totalEquity'), data.totalEquity),
    totalRow('sum', t('balanceSheet.sum'), data.totalLiabilities + data.totalEquity),
  ];

  return (
    <ReportPage<StatementRow, Filters>
      feature={BALANCE_SHEET_FEATURE}
      title={t('balanceSheet.title')}
      subtitle={t('balanceSheet.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      filters={[{ key: 'to', label: t('balanceSheet.asOf'), type: 'date' }]}
      stats={[
        {
          key: 'assets',
          icon: <BankOutlined />,
          tint: 'green',
          label: t('balanceSheet.totalAssets'),
          value: formatMoney(data?.totalAssets ?? 0),
        },
        {
          key: 'liabilities',
          icon: <CreditCardOutlined />,
          tint: 'amber',
          label: t('balanceSheet.totalLiabilities'),
          value: formatMoney(data?.totalLiabilities ?? 0),
        },
        {
          key: 'equity',
          icon: <PieChartOutlined />,
          tint: 'blue',
          label: t('balanceSheet.totalEquity'),
          value: formatMoney(data?.totalEquity ?? 0),
        },
      ]}
      columns={columns}
      rows={rows}
      total={rows?.length}
      rowKey={(row) => row.key}
      loading={query.isFetching}
      onRowClick={(row) =>
        row.accountId &&
        void navigate(accountLedgerLink(row.accountId, { from: null, to: list.filters.to }))
      }
      exportFile={t('balanceSheet.exportFile')}
      fetchAll={() => Promise.resolve(rows ?? [])}
      empty={{ title: t('report.emptyTitle'), description: t('report.emptyText') }}
    />
  );
}
