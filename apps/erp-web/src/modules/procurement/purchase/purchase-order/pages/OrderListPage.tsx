import {
  getListPurchaseOrdersQueryKey,
  listPurchaseOrders,
  useListPurchaseOrders,
  type PurchaseOrderRow,
} from '@erp/api-client';
import { useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { DocumentListPage } from '../../../components/DocumentListPage';
import { statusParam, type ApprovalView } from '../../../procurement';
import { ORDER_FEATURE, ORDER_LOOKUPS, orderPaths } from '../constants';
import { orderCalls, useOrderLookups } from '../order-form';

type Filters = {
  supplierId: string | null;
  warehouseId: string | null;
  paymentTypeId: string | null;
};

export function OrderListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(ORDER_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { supplierId: null, warehouseId: null, paymentTypeId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    supplierId: idParam(list.filters.supplierId),
    warehouseId: list.filters.warehouseId ?? undefined,
    paymentTypeId: idParam(list.filters.paymentTypeId),
  };
  const query = useListPurchaseOrders(params, {
    query: {
      queryKey: useUnitKey(getListPurchaseOrdersQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(ORDER_FEATURE, access.view);
  const lookups = useOrderLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    supplierId: lookups[ORDER_LOOKUPS.supplier],
    warehouseId: lookups[ORDER_LOOKUPS.warehouse],
    paymentTypeId: lookups[ORDER_LOOKUPS.paymentType],
  });

  return (
    <DocumentListPage<PurchaseOrderRow, Filters>
      feature={ORDER_FEATURE}
      config={config.data}
      title={t('order.title')}
      subtitle={t('order.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('order.new')}
      search={{ label: t('order.searchLabel'), placeholder: t('order.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('order.totals.net'), value: (row) => row.netAmount }}
      exportFile={t('order.exportFile')}
      fetchAll={() =>
        listPurchaseOrders({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('order.emptyTitle'), description: t('order.emptyText') }}
      paths={orderPaths}
      calls={orderCalls}
    />
  );
}
