import {
  getListPurchaseReceivesQueryKey,
  listPurchaseReceives,
  useListPurchaseReceives,
  type PurchaseReceiveRow,
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
import { RECEIVE_FEATURE, RECEIVE_LOOKUPS, receivePaths } from '../constants';
import { receiveCalls, useReceiveLookups } from '../receive-form';

type Filters = {
  supplierId: string | null;
  warehouseId: string | null;
  paymentTypeId: string | null;
};

export function ReceiveListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(RECEIVE_FEATURE);
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
  const query = useListPurchaseReceives(params, {
    query: {
      queryKey: useUnitKey(getListPurchaseReceivesQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(RECEIVE_FEATURE, access.view);
  const { lookups } = useReceiveLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    supplierId: lookups[RECEIVE_LOOKUPS.supplier],
    warehouseId: lookups[RECEIVE_LOOKUPS.warehouse],
    paymentTypeId: lookups[RECEIVE_LOOKUPS.paymentType],
  });

  return (
    <DocumentListPage<PurchaseReceiveRow, Filters>
      feature={RECEIVE_FEATURE}
      config={config.data}
      title={t('receive.title')}
      subtitle={t('receive.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('receive.new')}
      search={{ label: t('receive.searchLabel'), placeholder: t('receive.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('procurement.totals.net'), value: (row) => row.netAmount }}
      exportFile={t('receive.exportFile')}
      fetchAll={() =>
        listPurchaseReceives({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('receive.emptyTitle'), description: t('receive.emptyText') }}
      paths={receivePaths}
      calls={receiveCalls}
    />
  );
}
