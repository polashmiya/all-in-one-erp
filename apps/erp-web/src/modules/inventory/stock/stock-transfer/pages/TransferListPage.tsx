import {
  getListStockTransfersQueryKey,
  listStockTransfers,
  useListStockTransfers,
  type StockTransferRow,
} from '@erp/api-client';
import { formatNumber, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { DocumentListPage } from '@/common/documents/DocumentListPage';
import {
  QUANTITY_DECIMALS_SHOWN,
  statusParam,
  type ApprovalView,
} from '@/common/documents/documents';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { TRANSFER_FEATURE, TRANSFER_LOOKUPS, transferPaths } from '../constants';
import { transferCalls, useTransferLookups } from '../transfer-form';

type Filters = { fromWarehouseId: string | null; toWarehouseId: string | null };

export function TransferListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(TRANSFER_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { fromWarehouseId: null, toWarehouseId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    fromWarehouseId: list.filters.fromWarehouseId ?? undefined,
    toWarehouseId: list.filters.toWarehouseId ?? undefined,
  };
  const query = useListStockTransfers(params, {
    query: {
      queryKey: useUnitKey(getListStockTransfersQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(TRANSFER_FEATURE, access.view);
  const lookups = useTransferLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    fromWarehouseId: lookups[TRANSFER_LOOKUPS.warehouse],
    toWarehouseId: lookups[TRANSFER_LOOKUPS.warehouse],
  });

  return (
    <DocumentListPage<StockTransferRow, Filters>
      feature={TRANSFER_FEATURE}
      config={config.data}
      title={t('transfer.title')}
      subtitle={t('transfer.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('transfer.new')}
      search={{ label: t('transfer.searchLabel'), placeholder: t('transfer.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{
        label: t('document.col.quantity'),
        value: (row) => row.totalQuantity,
        format: (value) => formatNumber(value, QUANTITY_DECIMALS_SHOWN),
      }}
      lines={{
        label: t('document.col.items'),
        value: (row) => row.itemCount,
        subtitle: (row) => t('document.itemCount', { count: row.itemCount }),
      }}
      exportFile={t('transfer.exportFile')}
      fetchAll={() =>
        listStockTransfers({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('transfer.emptyTitle'), description: t('transfer.emptyText') }}
      paths={transferPaths}
      calls={transferCalls}
    />
  );
}
