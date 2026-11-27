import { FallOutlined, LineChartOutlined, RiseOutlined } from '@ant-design/icons';
import { getGetIncomeStatementQueryKey, useGetIncomeStatement } from '@erp/api-client';
import { formatMoney, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, useUnitKey } from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { ReportPage } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  totalRow,
  useGroupRows,
  useStatementColumns,
  type StatementRow,
} from '../../../components/statement';
import { accountLedgerLink } from '../../account-ledger/constants';
import { INCOME_STATEMENT_FEATURE } from '../constants';

export function IncomeStatementPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(INCOME_STATEMENT_FEATURE);
  const columns = useStatementColumns();
  const groupRows = useGroupRows();
  const dates = useDateFilters<DateRange>();
  const list = useListState<DateRange>({ filters: NO_DATES, pageSize: EXPORT_PAGE_SIZE });

  const params = dateParams(list.filters);
  const query = useGetIncomeStatement(params, {
    query: {
      queryKey: useUnitKey(getGetIncomeStatementQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const data = query.data;
  const rows: StatementRow[] | undefined = data && [
    ...groupRows(data.income),
    ...groupRows(data.costOfSales),
    totalRow('grossProfit', t('incomeStatement.grossProfit'), data.grossProfit),
    ...groupRows(data.expenses),
    totalRow('netProfit', t('incomeStatement.netProfit'), data.netProfit),
  ];

  return (
    <ReportPage<StatementRow, DateRange>
      feature={INCOME_STATEMENT_FEATURE}
      title={t('incomeStatement.title')}
      subtitle={t('incomeStatement.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      filters={dates}
      stats={[
        {
          key: 'income',
          icon: <RiseOutlined />,
          tint: 'green',
          label: t('incomeStatement.totalIncome'),
          value: formatMoney(data?.income.total ?? 0),
        },
        {
          key: 'expenses',
          icon: <FallOutlined />,
          tint: 'amber',
          label: t('incomeStatement.costAndExpenses'),
          value: formatMoney((data?.costOfSales.total ?? 0) + (data?.expenses.total ?? 0)),
        },
        {
          key: 'profit',
          icon: <LineChartOutlined />,
          tint: 'blue',
          label: t('incomeStatement.netProfit'),
          value: formatMoney(data?.netProfit ?? 0),
        },
      ]}
      columns={columns}
      rows={rows}
      total={rows?.length}
      rowKey={(row) => row.key}
      loading={query.isFetching}
      onRowClick={(row) =>
        row.accountId && void navigate(accountLedgerLink(row.accountId, list.filters))
      }
      exportFile={t('incomeStatement.exportFile')}
      fetchAll={() => Promise.resolve(rows ?? [])}
      empty={{ title: t('report.emptyTitle'), description: t('report.emptyText') }}
    />
  );
}
