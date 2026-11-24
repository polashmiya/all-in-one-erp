import {
  getGetSupplierLedgerQueryKey,
  getGetSupplierOptionsQueryKey,
  getSupplierLedger,
  useGetSupplierLedger,
  useGetSupplierOptions,
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
import { SUPPLIER_LEDGER_FEATURE } from '../constants';

type Filters = DateRange & { supplierId: string | null };

export function SupplierLedgerPage() {
  const { t } = useTranslation();
  const access = useFeatureAccess(SUPPLIER_LEDGER_FEATURE);
  const suppliers = useGetSupplierOptions({
    query: { queryKey: useUnitKey(getGetSupplierOptionsQueryKey()), enabled: access.view },
  });
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters>({
    filters: { supplierId: null, ...NO_DATES },
    pageSize: appConfig.defaultPageSize,
  });

  const supplierId = idParam(list.filters.supplierId) ?? 0;
  const params = {
    supplierId,
    ...dateParams(list.filters),
    page: list.page,
    pageSize: list.pageSize,
  };
  const query = useGetSupplierLedger(params, {
    query: {
      queryKey: useUnitKey(getGetSupplierLedgerQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view && supplierId > 0,
    },
  });
  const data = supplierId ? query.data : undefined;
  const { columns, stats } = useLedger(data, {
    key: 'account',
    title: t('accounting.account'),
    value: (row) => row.accountName,
  });

  return (
    <ReportPage<LedgerRow, Filters>
      feature={SUPPLIER_LEDGER_FEATURE}
      title={t('ledger.supplier.title')}
      subtitle={t('ledger.supplier.subtitle')}
      list={list}
      filters={[
        {
          key: 'supplierId',
          label: t('ledger.col.supplier'),
          options: (suppliers.data ?? []).map((s) => ({
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
      onRefresh={() => void query.refetch()}
      exportFile={t('ledger.supplier.exportFile')}
      fetchAll={() =>
        getSupplierLedger({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={
        supplierId
          ? { title: t('report.emptyTitle'), description: t('report.emptyText') }
          : { title: t('ledger.supplier.emptyTitle'), description: t('ledger.supplier.emptyText') }
      }
    />
  );
}
