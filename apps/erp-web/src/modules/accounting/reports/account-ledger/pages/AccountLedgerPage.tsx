import {
  getAccountLedger,
  getGetAccountLedgerQueryKey,
  useGetAccountLedger,
  type LedgerRow,
} from '@erp/api-client';
import { useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, idParam, lookupLabel, useUnitKey } from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { ReportPage } from '@/common/reports/ReportPage';
import { useAccountOptions } from '../../../accounting';
import { useLedger } from '../../../components/useLedger';
import { ACCOUNT_LEDGER_FEATURE } from '../constants';

type Filters = DateRange & { accountId: string | null };

export function AccountLedgerPage() {
  const { t } = useTranslation();
  const access = useFeatureAccess(ACCOUNT_LEDGER_FEATURE);
  const accounts = useAccountOptions(access.view);
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters>({
    filters: { accountId: null, ...NO_DATES },
    pageSize: appConfig.defaultPageSize,
  });

  const accountId = idParam(list.filters.accountId) ?? 0;
  const params = {
    accountId,
    ...dateParams(list.filters),
    page: list.page,
    pageSize: list.pageSize,
  };
  const query = useGetAccountLedger(params, {
    query: {
      queryKey: useUnitKey(getGetAccountLedgerQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view && accountId > 0,
    },
  });
  const data = accountId ? query.data : undefined;
  const { columns, stats } = useLedger(data, {
    key: 'supplier',
    title: t('ledger.col.supplier'),
    value: (row) => row.supplierName,
  });

  return (
    <ReportPage<LedgerRow, Filters>
      feature={ACCOUNT_LEDGER_FEATURE}
      title={t('ledger.account.title')}
      subtitle={t('ledger.account.subtitle')}
      list={list}
      filters={[
        {
          key: 'accountId',
          label: t('accounting.account'),
          options: (accounts ?? [])
            .filter((a) => !a.isGroup)
            .map((a) => ({ value: String(a.id), label: lookupLabel(a) })),
        },
        ...dates,
      ]}
      stats={stats}
      columns={columns}
      rows={data?.items}
      total={data?.total}
      rowKey={(row) => row.id}
      loading={query.isFetching}
      exportFile={t('ledger.account.exportFile')}
      fetchAll={() =>
        getAccountLedger({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={
        accountId
          ? { title: t('report.emptyTitle'), description: t('report.emptyText') }
          : { title: t('ledger.account.emptyTitle'), description: t('ledger.account.emptyText') }
      }
    />
  );
}
