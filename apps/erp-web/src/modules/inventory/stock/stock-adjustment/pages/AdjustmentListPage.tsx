import {
  getListStockAdjustmentsQueryKey,
  listStockAdjustments,
  useListStockAdjustments,
  type StockAdjustmentRow,
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
import { adjustmentCalls, useAdjustmentLookups } from '../adjustment-form';
import { ADJUSTMENT_FEATURE, ADJUSTMENT_LOOKUPS, adjustmentPaths } from '../constants';

type Filters = { type: string | null; warehouseId: string | null; reasonId: string | null };

export function AdjustmentListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(ADJUSTMENT_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { type: null, warehouseId: null, reasonId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    type: list.filters.type ?? undefined,
    warehouseId: list.filters.warehouseId ?? undefined,
    reasonId: idParam(list.filters.reasonId),
  };
  const query = useListStockAdjustments(params, {
    query: {
      queryKey: useUnitKey(getListStockAdjustmentsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(ADJUSTMENT_FEATURE, access.view);
  const lookups = useAdjustmentLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    type: lookups[ADJUSTMENT_LOOKUPS.type],
    warehouseId: lookups[ADJUSTMENT_LOOKUPS.warehouse],
    reasonId: lookups[ADJUSTMENT_LOOKUPS.reason],
  });
  const typeName = (row: StockAdjustmentRow) => t(`adjustment.type.${row.type}`);

  return (
    <DocumentListPage<StockAdjustmentRow, Filters>
      feature={ADJUSTMENT_FEATURE}
      config={config.data}
      title={t('adjustment.title')}
      subtitle={t('adjustment.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('adjustment.new')}
      search={{ label: t('adjustment.searchLabel'), placeholder: t('adjustment.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      amount={{ label: t('adjustment.totals.amount'), value: (row) => row.totalAmount }}
      renderers={{ type: { render: typeName, csv: typeName } }}
      exportFile={t('adjustment.exportFile')}
      fetchAll={() =>
        listStockAdjustments({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('adjustment.emptyTitle'), description: t('adjustment.emptyText') }}
      paths={adjustmentPaths}
      calls={adjustmentCalls}
    />
  );
}
