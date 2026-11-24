import { DollarOutlined, InboxOutlined, StopOutlined } from '@ant-design/icons';
import {
  getGetStockBalanceQueryKey,
  getStockBalance,
  useGetStockBalance,
  type StockBalanceRow,
} from '@erp/api-client';
import { formatMoney, formatNumber, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { QUANTITY_DECIMALS_SHOWN, useUnitWarehouses } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, listParams, useUnitKey } from '@/common/master-data';
import { ReportPage } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { stockLedgerLink } from '../../stock-ledger/constants';
import { STOCK_BALANCE_FEATURE, STOCK_VIEWS, type StockView } from '../constants';

type Filters = { warehouseId: string | null };

export function StockBalancePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(STOCK_BALANCE_FEATURE);
  const warehouses = useUnitWarehouses(access.view);
  const list = useListState<Filters, StockView>({
    filters: { warehouseId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const { search, sort, page, pageSize } = listParams({ ...list.query, view: undefined });
  const params = {
    search,
    sort,
    page,
    pageSize,
    view: list.query.view,
    warehouseId: list.filters.warehouseId ?? undefined,
  };
  const query = useGetStockBalance(params, {
    query: {
      queryKey: useUnitKey(getGetStockBalanceQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const data = query.data;

  return (
    <ReportPage<StockBalanceRow, Filters, StockView>
      feature={STOCK_BALANCE_FEATURE}
      title={t('stockBalance.title')}
      subtitle={t('stockBalance.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      search={{ label: t('stockBalance.search'), placeholder: t('stockBalance.search') }}
      filters={[
        {
          key: 'warehouseId',
          label: t('stockBalance.col.warehouse'),
          options: (warehouses ?? []).map((w) => ({ value: String(w.id), label: w.name })),
        },
      ]}
      views={STOCK_VIEWS.map((key) => ({
        key,
        label: t(`stockBalance.view.${key}`),
        count: data?.counts[key],
      }))}
      stats={[
        {
          key: 'value',
          icon: <DollarOutlined />,
          tint: 'green',
          label: t('stockBalance.stat.value'),
          value: formatMoney(data?.totalValue ?? 0),
        },
        {
          key: 'quantity',
          icon: <InboxOutlined />,
          tint: 'blue',
          label: t('stockBalance.stat.quantity'),
          value: formatNumber(data?.totalQuantity ?? 0, QUANTITY_DECIMALS_SHOWN),
        },
        {
          key: 'out',
          icon: <StopOutlined />,
          tint: 'amber',
          label: t('stockBalance.stat.outOfStock'),
          value: formatNumber(data?.counts.outOfStock ?? 0),
        },
      ]}
      columns={[
        {
          key: 'item',
          title: t('stockBalance.col.item'),
          sorter: true,
          value: (row) => `${row.itemName} (${row.itemCode})`,
          render: (row) => <TitleCell title={row.itemName} subtitle={row.itemCode} />,
        },
        {
          key: 'category',
          title: t('stockBalance.col.category'),
          value: (row) => row.categoryName,
        },
        {
          key: 'warehouse',
          title: t('stockBalance.col.warehouse'),
          value: (row) => row.warehouseName,
        },
        {
          key: 'quantity',
          title: t('stockBalance.col.quantity'),
          type: 'quantity',
          sorter: true,
          value: (row) => row.quantity,
        },
        { key: 'uom', title: t('stockBalance.col.uom'), value: (row) => row.uomName },
        {
          key: 'cost',
          title: t('stockBalance.col.cost'),
          type: 'money',
          value: (row) => row.cost,
        },
        {
          key: 'value',
          title: t('stockBalance.col.value'),
          type: 'money',
          sorter: true,
          value: (row) => row.value,
        },
      ]}
      rows={data?.items}
      total={data?.total}
      rowKey={(row) => `${row.itemId}:${row.warehouseId}`}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      onRowClick={(row) => navigate(stockLedgerLink(row.itemId, row.warehouseId))}
      exportFile={t('stockBalance.exportFile')}
      fetchAll={() =>
        getStockBalance({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('stockBalance.emptyTitle'), description: t('stockBalance.emptyText') }}
    />
  );
}
