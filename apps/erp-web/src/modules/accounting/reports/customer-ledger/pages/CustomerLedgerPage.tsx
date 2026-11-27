import {
  getGetCustomerLedgerQueryKey,
  getGetCustomerOptionsQueryKey,
  getCustomerLedger,
  useGetCustomerLedger,
  useGetCustomerOptions,
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
import { useLedger } from '../../../components/useLedger';
import { CUSTOMER_LEDGER_FEATURE } from '../constants';

type Filters = DateRange & { customerId: string | null };

export function CustomerLedgerPage() {
  const { t } = useTranslation();
  const access = useFeatureAccess(CUSTOMER_LEDGER_FEATURE);
  const customers = useGetCustomerOptions({
    query: { queryKey: useUnitKey(getGetCustomerOptionsQueryKey()), enabled: access.view },
  });
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters>({
    filters: { customerId: null, ...NO_DATES },
    pageSize: appConfig.defaultPageSize,
  });

  const customerId = idParam(list.filters.customerId) ?? 0;
  const params = {
    customerId,
    ...dateParams(list.filters),
    page: list.page,
    pageSize: list.pageSize,
  };
  const query = useGetCustomerLedger(params, {
    query: {
      queryKey: useUnitKey(getGetCustomerLedgerQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view && customerId > 0,
    },
  });
  const data = customerId ? query.data : undefined;
  const { columns, stats } = useLedger(data, {
    key: 'account',
    title: t('accounting.account'),
    value: (row) => row.accountName,
  });

  return (
    <ReportPage<LedgerRow, Filters>
      feature={CUSTOMER_LEDGER_FEATURE}
      title={t('ledger.customer.title')}
      subtitle={t('ledger.customer.subtitle')}
      list={list}
      filters={[
        {
          key: 'customerId',
          label: t('ledger.col.customer'),
          options: (customers.data ?? []).map((s) => ({
            value: String(s.id),
            label: lookupLabel(s),
          })),
        },
        ...dates,
      ]}
      stats={stats}
      columns={columns}
      rows={data?.items}
      total={data?.total}
      rowKey={(row) => row.id}
      loading={query.isFetching}
      exportFile={t('ledger.customer.exportFile')}
      fetchAll={() =>
        getCustomerLedger({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={
        customerId
          ? { title: t('report.emptyTitle'), description: t('report.emptyText') }
          : { title: t('ledger.customer.emptyTitle'), description: t('ledger.customer.emptyText') }
      }
    />
  );
}
