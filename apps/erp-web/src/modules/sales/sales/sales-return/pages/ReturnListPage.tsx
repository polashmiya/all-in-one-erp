import {
  getListSalesReturnsQueryKey,
  listSalesReturns,
  useListSalesReturns,
  type SalesReturnRow,
} from '@erp/api-client';
import { useListState } from '@erp/ui';
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
import { RETURN_FEATURE, RETURN_LOOKUPS, returnPaths } from '../constants';
import { returnCalls, useReturnLookups } from '../return-form';

type Filters = {
  source: string | null;
  customerId: string | null;
  warehouseId: string | null;
  returnReasonId: string | null;
};

export function ReturnListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(RETURN_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { source: null, customerId: null, warehouseId: null, returnReasonId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    source: list.filters.source ?? undefined,
    customerId: idParam(list.filters.customerId),
    warehouseId: list.filters.warehouseId ?? undefined,
    returnReasonId: idParam(list.filters.returnReasonId),
  };
  const query = useListSalesReturns(params, {
    query: {
      queryKey: useUnitKey(getListSalesReturnsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(RETURN_FEATURE, access.view);
  const { lookups } = useReturnLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    source: lookups[RETURN_LOOKUPS.source],
    customerId: lookups[RETURN_LOOKUPS.customer],
    warehouseId: lookups[RETURN_LOOKUPS.warehouse],
    returnReasonId: lookups[RETURN_LOOKUPS.reason],
  });

  const sourceName = (row: SalesReturnRow) => t(`salesReturn.source.${row.source}`);

  return (
    <DocumentListPage<SalesReturnRow, Filters>
      feature={RETURN_FEATURE}
      config={config.data}
      title={t('salesReturn.title')}
      subtitle={t('salesReturn.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('salesReturn.new')}
      search={{ label: t('salesReturn.searchLabel'), placeholder: t('salesReturn.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      amount={{ label: t('sales.totals.total'), value: (row) => row.netAmount }}
      renderers={{ source: { render: sourceName, csv: sourceName } }}
      exportFile={t('salesReturn.exportFile')}
      fetchAll={() =>
        listSalesReturns({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('salesReturn.emptyTitle'), description: t('salesReturn.emptyText') }}
      paths={returnPaths}
      calls={returnCalls}
    />
  );
}
