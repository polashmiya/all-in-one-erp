import {
  getListPurchaseReturnsQueryKey,
  listPurchaseReturns,
  useListPurchaseReturns,
  type PurchaseReturnRow,
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
import { RETURN_FEATURE, RETURN_LOOKUPS, returnPaths } from '../constants';
import { returnCalls, useReturnLookups } from '../return-form';

type Filters = {
  supplierId: string | null;
  warehouseId: string | null;
  reasonId: string | null;
};

export function ReturnListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(RETURN_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { supplierId: null, warehouseId: null, reasonId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    supplierId: idParam(list.filters.supplierId),
    warehouseId: list.filters.warehouseId ?? undefined,
    reasonId: idParam(list.filters.reasonId),
  };
  const query = useListPurchaseReturns(params, {
    query: {
      queryKey: useUnitKey(getListPurchaseReturnsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(RETURN_FEATURE, access.view);
  const { lookups } = useReturnLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    supplierId: lookups[RETURN_LOOKUPS.supplier],
    warehouseId: lookups[RETURN_LOOKUPS.warehouse],
    reasonId: lookups[RETURN_LOOKUPS.reason],
  });

  return (
    <DocumentListPage<PurchaseReturnRow, Filters>
      feature={RETURN_FEATURE}
      config={config.data}
      title={t('return.title')}
      subtitle={t('return.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('return.new')}
      search={{ label: t('return.searchLabel'), placeholder: t('return.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('procurement.totals.net'), value: (row) => row.netAmount }}
      exportFile={t('return.exportFile')}
      fetchAll={() =>
        listPurchaseReturns({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('return.emptyTitle'), description: t('return.emptyText') }}
      paths={returnPaths}
      calls={returnCalls}
    />
  );
}
