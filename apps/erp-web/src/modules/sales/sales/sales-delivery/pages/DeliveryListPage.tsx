import {
  getListSalesDeliveriesQueryKey,
  listSalesDeliveries,
  useListSalesDeliveries,
  type SalesDeliveryRow,
} from '@erp/api-client';
import { formatMoney, useListState } from '@erp/ui';
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
import { DELIVERY_FEATURE, DELIVERY_LOOKUPS, deliveryPaths } from '../constants';
import { deliveryCalls, useDeliveryLookups } from '../delivery-form';

type Filters = {
  type: string | null;
  customerId: string | null;
  warehouseId: string | null;
  salesForceId: string | null;
  paymentTypeId: string | null;
};

export function DeliveryListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(DELIVERY_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: {
      type: null,
      customerId: null,
      warehouseId: null,
      salesForceId: null,
      paymentTypeId: null,
    },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    type: list.filters.type ?? undefined,
    customerId: idParam(list.filters.customerId),
    warehouseId: list.filters.warehouseId ?? undefined,
    salesForceId: idParam(list.filters.salesForceId),
    paymentTypeId: idParam(list.filters.paymentTypeId),
  };
  const query = useListSalesDeliveries(params, {
    query: {
      queryKey: useUnitKey(getListSalesDeliveriesQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(DELIVERY_FEATURE, access.view);
  const { lookups } = useDeliveryLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    type: lookups[DELIVERY_LOOKUPS.type],
    customerId: lookups[DELIVERY_LOOKUPS.customer],
    warehouseId: lookups[DELIVERY_LOOKUPS.warehouse],
    salesForceId: lookups[DELIVERY_LOOKUPS.employee],
    paymentTypeId: lookups['payment-type'],
  });
  const typeName = (row: SalesDeliveryRow) => t(`salesDelivery.type.${row.type}`);

  return (
    <DocumentListPage<SalesDeliveryRow, Filters>
      feature={DELIVERY_FEATURE}
      config={config.data}
      title={t('salesDelivery.title')}
      subtitle={t('salesDelivery.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('salesDelivery.new')}
      search={{ label: t('salesDelivery.searchLabel'), placeholder: t('salesDelivery.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('sales.totals.total'), value: (row) => row.netAmount }}
      renderers={{ type: { render: typeName, csv: typeName } }}
      lines={{
        label: t('salesDelivery.due'),
        value: (row) => row.pendingAmount,
        subtitle: (row) =>
          [
            row.pendingAmount > 0
              ? t('salesDelivery.pending', { amount: formatMoney(row.pendingAmount) })
              : t('salesDelivery.paid'),
            row.isConfirmed && t('salesDelivery.confirmed'),
          ]
            .filter(Boolean)
            .join(' · '),
      }}
      exportFile={t('salesDelivery.exportFile')}
      fetchAll={() =>
        listSalesDeliveries({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('salesDelivery.emptyTitle'), description: t('salesDelivery.emptyText') }}
      paths={deliveryPaths}
      calls={deliveryCalls}
    />
  );
}
