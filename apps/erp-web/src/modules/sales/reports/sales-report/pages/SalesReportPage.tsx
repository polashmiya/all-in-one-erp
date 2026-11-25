import {
  LineChartOutlined,
  RollbackOutlined,
  ShoppingOutlined,
  WalletOutlined,
} from '@ant-design/icons';
import {
  getGetSalesReportQueryKey,
  getSalesReport,
  useGetSalesReport,
  type SalesReportRow,
} from '@erp/api-client';
import { formatMoney, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { ReportPage, type ReportColumn } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { DELIVERY_TYPES, deliveryPaths } from '../../../sales/sales-delivery/constants';
import { SALES_LOOKUPS } from '../../../sales-document';
import { useReportLookups } from '../../useReportLookups';
import { SALES_REPORT_FEATURE, SALES_REPORT_VIEWS, type SalesView } from '../constants';

type Filters = DateRange & {
  customerId: string | null;
  salesForceId: string | null;
  warehouseId: string | null;
  officeId: string | null;
  type: string | null;
};

const ITEM_VIEWS: SalesView[] = ['item', 'category'];

export function SalesReportPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(SALES_REPORT_FEATURE);
  const options = useReportLookups([], access.view);
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters, SalesView>({
    filters: {
      customerId: null,
      salesForceId: null,
      warehouseId: null,
      officeId: null,
      type: null,
      ...NO_DATES,
    },
    view: 'delivery',
    pageSize: appConfig.defaultPageSize,
  });

  const view = list.query.view ?? 'delivery';
  const { search, page, pageSize } = listParams({ ...list.query, view: undefined });
  const params = {
    view,
    search,
    page,
    pageSize,
    type: list.filters.type ?? undefined,
    customerId: idParam(list.filters.customerId),
    salesForceId: idParam(list.filters.salesForceId),
    warehouseId: list.filters.warehouseId ?? undefined,
    officeId: list.filters.officeId ?? undefined,
    ...dateParams(list.filters),
  };
  const query = useGetSalesReport(params, {
    query: {
      queryKey: useUnitKey(getGetSalesReportQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const totals = query.data?.totals;
  const byItem = ITEM_VIEWS.includes(view);

  const money = (
    key: string,
    value: (row: SalesReportRow) => number | null,
  ): ReportColumn<SalesReportRow> => ({
    key,
    title: t(`salesReport.col.${key}`),
    type: 'money',
    value,
  });
  const identity: ReportColumn<SalesReportRow>[] =
    view === 'delivery'
      ? [
          { key: 'delivery', title: t('salesReport.col.delivery'), value: (row) => row.code },
          {
            key: 'date',
            title: t('salesReport.col.date'),
            type: 'date',
            value: (row) => row.date,
          },
          { key: 'customer', title: t('salesReport.col.customer'), value: (row) => row.name },
        ]
      : [
          {
            key: view,
            title: t(`salesReport.col.${view}`),
            value: (row) =>
              [row.name ?? t('salesReport.none'), row.code].filter(Boolean).join(' · '),
            render: (row) => (
              <TitleCell title={row.name ?? t('salesReport.none')} subtitle={row.code} />
            ),
          },
          {
            key: 'deliveries',
            title: t('salesReport.col.deliveries'),
            type: 'quantity',
            value: (row) => row.deliveries,
          },
        ];

  return (
    <ReportPage<SalesReportRow, Filters, SalesView>
      feature={SALES_REPORT_FEATURE}
      title={t('salesReport.title')}
      subtitle={t('salesReport.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      search={{ label: t('salesReport.search'), placeholder: t('salesReport.search') }}
      filters={[
        {
          key: 'customerId',
          label: t('salesReport.col.customer'),
          options: options(SALES_LOOKUPS.customer),
        },
        {
          key: 'salesForceId',
          label: t('salesReport.col.salesForce'),
          options: options(SALES_LOOKUPS.employee),
        },
        {
          key: 'warehouseId',
          label: t('salesReport.col.warehouse'),
          options: options(SALES_LOOKUPS.warehouse),
        },
        {
          key: 'officeId',
          label: t('salesReport.col.office'),
          options: options(SALES_LOOKUPS.office),
        },
        {
          key: 'type',
          label: t('salesReport.col.type'),
          options: DELIVERY_TYPES.map((value) => ({
            value,
            label: t(`salesDelivery.type.${value}`),
          })),
        },
        ...dates,
      ]}
      views={SALES_REPORT_VIEWS.map((key) => ({ key, label: t(`salesReport.view.${key}`) }))}
      stats={[
        {
          key: 'sales',
          icon: <ShoppingOutlined />,
          tint: 'blue',
          label: t('salesReport.col.sales'),
          value: formatMoney(totals?.salesAmount ?? 0),
          caption: byItem
            ? undefined
            : t('salesReport.stat.deliveries', { count: totals?.deliveries ?? 0 }),
        },
        {
          key: 'returned',
          icon: <RollbackOutlined />,
          tint: 'amber',
          label: t('salesReport.col.returnAmount'),
          value: formatMoney(totals?.returnAmount ?? 0),
        },
        {
          key: 'profit',
          icon: <LineChartOutlined />,
          tint: 'green',
          label: t('salesReport.col.profit'),
          value: formatMoney(totals?.profit ?? 0),
          caption: `${t('salesReport.col.cost')}: ${formatMoney(totals?.cost ?? 0)}`,
        },
        ...(byItem
          ? []
          : [
              {
                key: 'due',
                icon: <WalletOutlined />,
                tint: 'violet' as const,
                label: t('salesReport.col.due'),
                value: formatMoney(totals?.due ?? 0),
                caption: `${t('salesReport.col.received')}: ${formatMoney(totals?.received ?? 0)}`,
              },
            ]),
      ]}
      columns={[
        ...identity,
        {
          key: 'quantity',
          title: t('salesReport.col.quantity'),
          type: 'quantity',
          value: (row) => row.quantity,
        },
        {
          key: 'returned',
          title: t('salesReport.col.returned'),
          type: 'quantity',
          value: (row) => row.returnedQuantity || null,
        },
        money('gross', (row) => row.grossAmount),
        money('discount', (row) => row.discount || null),
        money('sd', (row) => row.sdAmount || null),
        money('vat', (row) => row.vatAmount || null),
        money('net', (row) => row.netAmount),
        money('returnAmount', (row) => row.returnAmount || null),
        money('sales', (row) => row.salesAmount),
        money('cost', (row) => row.cost),
        money('profit', (row) => row.profit),
        ...(byItem
          ? []
          : [money('received', (row) => row.received), money('due', (row) => row.due)]),
      ]}
      rows={query.data?.items}
      total={query.data?.total}
      rowKey={(row) => `${view}:${row.key}`}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      onRowClick={
        view === 'delivery'
          ? (row) => row.id !== null && void navigate(deliveryPaths.view(row.id))
          : undefined
      }
      exportFile={t('salesReport.exportFile')}
      fetchAll={() =>
        getSalesReport({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('report.emptyTitle'), description: t('report.emptyText') }}
    />
  );
}
