import {
  getListSalesOrdersQueryKey,
  listSalesOrders,
  useListSalesOrders,
  type SalesOrderRow,
} from '@erp/api-client';
import { formatNumber, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { DocumentListPage } from '@/common/documents/DocumentListPage';
import { statusParam, type ApprovalView } from '@/common/documents/documents';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { ORDER_FEATURE, ORDER_LOOKUPS, orderPaths } from '../constants';
import { orderCalls, useOrderLookups } from '../order-form';

type Filters = {
  customerId: string | null;
  warehouseId: string | null;
  salesForceId: string | null;
  paymentTypeId: string | null;
};

export function OrderListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(ORDER_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { customerId: null, warehouseId: null, salesForceId: null, paymentTypeId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    customerId: idParam(list.filters.customerId),
    warehouseId: list.filters.warehouseId ?? undefined,
    salesForceId: idParam(list.filters.salesForceId),
    paymentTypeId: idParam(list.filters.paymentTypeId),
  };
  const query = useListSalesOrders(params, {
    query: {
      queryKey: useUnitKey(getListSalesOrdersQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(ORDER_FEATURE, access.view);
  const { lookups } = useOrderLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    customerId: lookups[ORDER_LOOKUPS.customer],
    warehouseId: lookups[ORDER_LOOKUPS.warehouse],
    salesForceId: lookups[ORDER_LOOKUPS.employee],
    paymentTypeId: lookups['payment-type'],
  });

  return (
    <DocumentListPage<SalesOrderRow, Filters>
      feature={ORDER_FEATURE}
      config={config.data}
      title={t('salesOrder.title')}
      subtitle={t('salesOrder.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('salesOrder.new')}
      search={{ label: t('salesOrder.searchLabel'), placeholder: t('salesOrder.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      amount={{ label: t('sales.totals.total'), value: (row) => row.netAmount }}
      lines={{
        label: t('salesOrder.lines.delivered'),
        value: (row) => row.deliveredQuantity,
        subtitle: (row) =>
          row.isCompleted
            ? t('salesOrder.completed')
            : t('salesOrder.deliveredOf', {
                delivered: formatNumber(row.deliveredQuantity),
                total: formatNumber(row.totalQuantity),
              }),
      }}
      exportFile={t('salesOrder.exportFile')}
      fetchAll={() =>
        listSalesOrders({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('salesOrder.emptyTitle'), description: t('salesOrder.emptyText') }}
      paths={orderPaths}
      calls={orderCalls}
    />
  );
}
