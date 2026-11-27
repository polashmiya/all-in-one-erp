import { CheckCircleOutlined, WarningOutlined } from '@ant-design/icons';
import {
  getGetTrialBalanceQueryKey,
  useGetTrialBalance,
  type TrialBalanceRow,
} from '@erp/api-client';
import { formatMoney, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, useUnitKey } from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { ReportPage } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { useBalanceText } from '../../../components/useLedger';
import { accountLedgerLink } from '../../account-ledger/constants';
import { TRIAL_BALANCE_FEATURE } from '../constants';

export function TrialBalancePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(TRIAL_BALANCE_FEATURE);
  const balance = useBalanceText();
  const dates = useDateFilters<DateRange>();
  const list = useListState<DateRange>({ filters: NO_DATES, pageSize: EXPORT_PAGE_SIZE });

  const params = { search: list.query.search || undefined, ...dateParams(list.filters) };
  const query = useGetTrialBalance(params, {
    query: {
      queryKey: useUnitKey(getGetTrialBalanceQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const data = query.data;
  const balanced = data?.closingDebit === data?.closingCredit;

  return (
    <ReportPage<TrialBalanceRow, DateRange>
      feature={TRIAL_BALANCE_FEATURE}
      title={t('trialBalance.title')}
      subtitle={t('trialBalance.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      search={{ label: t('trialBalance.search'), placeholder: t('trialBalance.search') }}
      filters={dates}
      stats={[
        {
          key: 'debit',
          icon: balanced ? <CheckCircleOutlined /> : <WarningOutlined />,
          tint: balanced ? 'green' : 'amber',
          label: t('trialBalance.stat.debit'),
          value: formatMoney(data?.closingDebit ?? 0),
        },
        {
          key: 'credit',
          icon: balanced ? <CheckCircleOutlined /> : <WarningOutlined />,
          tint: balanced ? 'green' : 'amber',
          label: t('trialBalance.stat.credit'),
          value: formatMoney(data?.closingCredit ?? 0),
        },
      ]}
      columns={[
        {
          key: 'account',
          title: t('accounting.account'),
          value: (row) => `${row.name} (${row.code})`,
          render: (row) => <TitleCell title={row.name} subtitle={row.code} />,
        },
        {
          key: 'type',
          title: t('trialBalance.col.type'),
          value: (row) => t(`accounting.type.${row.type}`),
        },
        {
          key: 'opening',
          title: t('accounting.opening'),
          type: 'money',
          value: (row) => row.opening,
          render: (row) => balance(row.opening),
        },
        {
          key: 'debit',
          title: t('accounting.debit'),
          type: 'money',
          value: (row) => row.debit || null,
        },
        {
          key: 'credit',
          title: t('accounting.credit'),
          type: 'money',
          value: (row) => row.credit || null,
        },
        {
          key: 'closingDebit',
          title: t('trialBalance.col.closingDebit'),
          type: 'money',
          value: (row) => (row.closing > 0 ? row.closing : null),
        },
        {
          key: 'closingCredit',
          title: t('trialBalance.col.closingCredit'),
          type: 'money',
          value: (row) => (row.closing < 0 ? -row.closing : null),
        },
      ]}
      rows={data?.items}
      total={data?.total}
      rowKey={(row) => row.id}
      loading={query.isFetching}
      onRowClick={(row) => navigate(accountLedgerLink(row.id, list.filters))}
      exportFile={t('trialBalance.exportFile')}
      fetchAll={() => Promise.resolve(data?.items ?? [])}
      empty={{ title: t('report.emptyTitle'), description: t('report.emptyText') }}
    />
  );
}
