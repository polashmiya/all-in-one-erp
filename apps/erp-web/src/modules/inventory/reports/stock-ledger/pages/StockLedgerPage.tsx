import { ArrowDownOutlined, ArrowUpOutlined, InboxOutlined } from '@ant-design/icons';
import {
  getGetStockLedgerQueryKey,
  getStockLedger,
  useGetStockLedger,
  type StockLedgerRow,
} from '@erp/api-client';
import { formatMoney, formatNumber, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { QUANTITY_DECIMALS_SHOWN, useUnitWarehouses } from '@/common/documents/documents';
import { ItemSelect } from '@/common/documents/ItemSelect';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, idParam, useUnitKey } from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { ReportPage } from '@/common/reports/ReportPage';
import { documentLink } from '@/modules/document-registry';
import { STOCK_LEDGER_FEATURE } from '../constants';

type Filters = DateRange & { itemId: string | null; warehouseId: string | null };

export function StockLedgerPage() {
  const { t } = useTranslation();
  const access = useFeatureAccess(STOCK_LEDGER_FEATURE);
  const warehouses = useUnitWarehouses(access.view);
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters>({
    filters: { itemId: null, warehouseId: null, ...NO_DATES },
    pageSize: appConfig.defaultPageSize,
  });

  const itemId = idParam(list.filters.itemId) ?? 0;
  const params = {
    itemId,
    warehouseId: list.filters.warehouseId ?? undefined,
    ...dateParams(list.filters),
    page: list.page,
    pageSize: list.pageSize,
  };
  const query = useGetStockLedger(params, {
    query: {
      queryKey: useUnitKey(getGetStockLedgerQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view && itemId > 0,
    },
  });
  const data = itemId ? query.data : undefined;
  const quantity = (value: number | undefined) => formatNumber(value ?? 0, QUANTITY_DECIMALS_SHOWN);
  const typeName = (row: StockLedgerRow) => t(documentLink(row.feature)?.label ?? row.feature);

  return (
    <ReportPage<StockLedgerRow, Filters>
      feature={STOCK_LEDGER_FEATURE}
      title={t('stockLedger.title')}
      subtitle={t('stockLedger.subtitle')}
      list={list}
      filters={[
        {
          key: 'itemId',
          label: t('stockLedger.filter.item'),
          render: (value, onChange) => (
            <ItemSelect
              value={value ? Number(value) : null}
              picked={
                data?.itemName
                  ? { id: itemId, code: data.itemCode ?? undefined, name: data.itemName }
                  : undefined
              }
              placeholder={t('stockLedger.filter.item')}
              onPick={(option) => onChange(String(option.id))}
            />
          ),
        },
        {
          key: 'warehouseId',
          label: t('stockLedger.col.warehouse'),
          options: (warehouses ?? []).map((w) => ({ value: String(w.id), label: w.name })),
        },
        ...dates,
      ]}
      stats={[
        {
          key: 'in',
          icon: <ArrowDownOutlined />,
          tint: 'green',
          label: t('stockLedger.stat.in'),
          value: quantity(data?.quantityIn),
          caption: t('stockLedger.stat.opening', { quantity: quantity(data?.openingQuantity) }),
        },
        {
          key: 'out',
          icon: <ArrowUpOutlined />,
          tint: 'amber',
          label: t('stockLedger.stat.out'),
          value: quantity(data?.quantityOut),
        },
        {
          key: 'closing',
          icon: <InboxOutlined />,
          tint: 'blue',
          label: t('stockLedger.stat.closing'),
          value: quantity(data?.closingQuantity),
          caption: formatMoney(data?.closingValue ?? 0),
        },
      ]}
      columns={[
        { key: 'date', title: t('stockLedger.col.date'), type: 'date', value: (row) => row.date },
        {
          key: 'document',
          title: t('stockLedger.col.document'),
          value: (row) => row.documentCode,
          render: (row) => (
            <TitleCell
              title={
                <Link to={documentLink(row.feature)?.path(row.documentId) ?? '.'}>
                  {row.documentCode}
                </Link>
              }
              subtitle={typeName(row)}
            />
          ),
        },
        { key: 'supplier', title: t('stockLedger.col.party'), value: (row) => row.supplierName },
        {
          key: 'warehouse',
          title: t('stockLedger.col.warehouse'),
          value: (row) => row.warehouseName,
        },
        {
          key: 'in',
          title: t('stockLedger.col.in'),
          type: 'quantity',
          value: (row) => row.quantityIn || null,
        },
        {
          key: 'out',
          title: t('stockLedger.col.out'),
          type: 'quantity',
          value: (row) => row.quantityOut || null,
        },
        {
          key: 'value',
          title: t('stockLedger.col.value'),
          type: 'money',
          value: (row) => row.value,
        },
        {
          key: 'balance',
          title: t('stockLedger.col.balance'),
          type: 'quantity',
          value: (row) => row.balanceQuantity,
        },
        {
          key: 'balanceValue',
          title: t('stockLedger.col.balanceValue'),
          type: 'money',
          value: (row) => row.balanceValue,
        },
      ]}
      rows={data?.items}
      total={data?.total}
      rowKey={(row) => row.id}
      loading={query.isFetching}
      exportFile={t('stockLedger.exportFile')}
      fetchAll={() =>
        getStockLedger({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={
        itemId
          ? { title: t('report.emptyTitle'), description: t('report.emptyText') }
          : { title: t('stockLedger.emptyTitle'), description: t('stockLedger.emptyText') }
      }
    />
  );
}
