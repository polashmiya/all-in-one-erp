import { RollbackOutlined, ShoppingOutlined, WalletOutlined } from '@ant-design/icons';
import {
  getGetPurchaseReportQueryKey,
  getGetSupplierOptionsQueryKey,
  getPurchaseReport,
  useGetPurchaseReport,
  useGetSupplierOptions,
  type PurchaseReportRow,
} from '@erp/api-client';
import { formatMoney, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useUnitWarehouses } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import {
  EXPORT_PAGE_SIZE,
  idParam,
  listParams,
  lookupLabel,
  useUnitKey,
} from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { ReportPage, type ReportColumn } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { receivePaths } from '../../../purchase/purchase-receive/constants';
import { PURCHASE_REPORT_FEATURE, PURCHASE_VIEWS, type PurchaseView } from '../constants';

type Filters = DateRange & { supplierId: string | null; warehouseId: string | null };

export function PurchaseReportPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(PURCHASE_REPORT_FEATURE);
  const warehouses = useUnitWarehouses(access.view);
  const suppliers = useGetSupplierOptions({
    query: { queryKey: useUnitKey(getGetSupplierOptionsQueryKey()), enabled: access.view },
  });
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters, PurchaseView>({
    filters: { supplierId: null, warehouseId: null, ...NO_DATES },
    view: 'receive',
    pageSize: appConfig.defaultPageSize,
  });

  const view = list.query.view ?? 'receive';
  const { search, page, pageSize } = listParams({ ...list.query, view: undefined });
  const params = {
    view,
    search,
    page,
    pageSize,
    supplierId: idParam(list.filters.supplierId),
    warehouseId: list.filters.warehouseId ?? undefined,
    ...dateParams(list.filters),
  };
  const query = useGetPurchaseReport(params, {
    query: {
      queryKey: useUnitKey(getGetPurchaseReportQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const totals = query.data?.totals;

  const identity: ReportColumn<PurchaseReportRow>[] =
    view === 'receive'
      ? [
          {
            key: 'receive',
            title: t('purchaseReport.col.receive'),
            value: (row) => row.code,
          },
          {
            key: 'date',
            title: t('purchaseReport.col.date'),
            type: 'date',
            value: (row) => row.date,
          },
          { key: 'supplier', title: t('purchaseReport.col.supplier'), value: (row) => row.name },
        ]
      : [
          {
            key: view,
            title: t(`purchaseReport.col.${view}`),
            value: (row) => `${row.name} (${row.code})`,
            render: (row) => <TitleCell title={row.name} subtitle={row.code} />,
          },
          {
            key: 'receives',
            title: t('purchaseReport.col.receives'),
            type: 'quantity',
            value: (row) => row.receives,
          },
        ];
  const payments: ReportColumn<PurchaseReportRow>[] =
    view === 'item'
      ? []
      : [
          {
            key: 'paid',
            title: t('purchaseReport.col.paid'),
            type: 'money',
            value: (row) => row.paidAmount,
          },
          {
            key: 'due',
            title: t('purchaseReport.col.due'),
            type: 'money',
            value: (row) => row.due,
          },
        ];

  return (
    <ReportPage<PurchaseReportRow, Filters, PurchaseView>
      feature={PURCHASE_REPORT_FEATURE}
      title={t('purchaseReport.title')}
      subtitle={t('purchaseReport.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      search={{ label: t('purchaseReport.search'), placeholder: t('purchaseReport.search') }}
      filters={[
        {
          key: 'supplierId',
          label: t('purchaseReport.col.supplier'),
          options: (suppliers.data ?? []).map((s) => ({
            value: String(s.id),
            label: lookupLabel(s),
          })),
        },
        {
          key: 'warehouseId',
          label: t('purchaseReport.col.warehouse'),
          options: (warehouses ?? []).map((w) => ({ value: String(w.id), label: w.name })),
        },
        ...dates,
      ]}
      views={PURCHASE_VIEWS.map((key) => ({ key, label: t(`purchaseReport.view.${key}`) }))}
      stats={[
        {
          key: 'purchase',
          icon: <ShoppingOutlined />,
          tint: 'blue',
          label: t('purchaseReport.stat.purchase'),
          value: formatMoney(totals?.purchaseAmount ?? 0),
        },
        {
          key: 'returned',
          icon: <RollbackOutlined />,
          tint: 'amber',
          label: t('purchaseReport.stat.returned'),
          value: formatMoney(totals?.returnAmount ?? 0),
        },
        ...(view === 'item'
          ? []
          : [
              {
                key: 'due',
                icon: <WalletOutlined />,
                tint: 'green' as const,
                label: t('purchaseReport.col.due'),
                value: formatMoney(totals?.due ?? 0),
                caption: `${t('purchaseReport.col.paid')}: ${formatMoney(totals?.paidAmount ?? 0)}`,
              },
            ]),
      ]}
      columns={[
        ...identity,
        {
          key: 'quantity',
          title: t('purchaseReport.col.quantity'),
          type: 'quantity',
          value: (row) => row.quantity,
        },
        {
          key: 'returned',
          title: t('purchaseReport.col.returned'),
          type: 'quantity',
          value: (row) => row.returnedQuantity || null,
        },
        {
          key: 'vat',
          title: t('purchaseReport.col.vat'),
          type: 'money',
          value: (row) => row.vatAmount,
        },
        {
          key: 'net',
          title: t('purchaseReport.col.net'),
          type: 'money',
          value: (row) => row.netAmount,
        },
        {
          key: 'returnAmount',
          title: t('purchaseReport.col.returnAmount'),
          type: 'money',
          value: (row) => row.returnAmount || null,
        },
        {
          key: 'purchase',
          title: t('purchaseReport.col.purchase'),
          type: 'money',
          value: (row) => row.purchaseAmount,
        },
        ...payments,
      ]}
      rows={query.data?.items}
      total={query.data?.total}
      rowKey={(row) => `${view}:${row.id}`}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      onRowClick={
        view === 'receive' ? (row) => void navigate(receivePaths.view(row.id)) : undefined
      }
      exportFile={t('purchaseReport.exportFile')}
      fetchAll={() =>
        getPurchaseReport({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('report.emptyTitle'), description: t('report.emptyText') }}
    />
  );
}
