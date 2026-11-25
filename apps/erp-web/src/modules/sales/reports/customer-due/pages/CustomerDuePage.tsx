import { ClockCircleOutlined, FileTextOutlined, WalletOutlined } from '@ant-design/icons';
import {
  getCustomerDueReport,
  getGetCustomerDueReportQueryKey,
  useGetCustomerDueReport,
  type CustomerDueRow,
} from '@erp/api-client';
import { formatMoney, formatNumber, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { ReportPage, type ReportColumn } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { COLLECTION_FEATURE, collectionPaths } from '../../../sales/sales-collection/constants';
import { deliveryPaths } from '../../../sales/sales-delivery/constants';
import { SALES_LOOKUPS } from '../../../sales-document';
import { useReportLookups } from '../../useReportLookups';
import { CUSTOMER_DUE_FEATURE, CUSTOMER_DUE_VIEWS, type DueView } from '../constants';

type Filters = {
  customerId: string | null;
  salesForceId: string | null;
  overdue: string | null;
};

const YES = 'yes';

export function CustomerDuePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(CUSTOMER_DUE_FEATURE);
  const collections = useFeatureAccess(COLLECTION_FEATURE);
  const options = useReportLookups([], access.view);
  const list = useListState<Filters, DueView>({
    filters: { customerId: null, salesForceId: null, overdue: null },
    view: 'customer',
    pageSize: appConfig.defaultPageSize,
  });

  const view = list.query.view ?? 'customer';
  const { search, page, pageSize } = listParams({ ...list.query, view: undefined });
  const params = {
    view,
    search,
    page,
    pageSize,
    customerId: idParam(list.filters.customerId),
    salesForceId: idParam(list.filters.salesForceId),
    overdue: list.filters.overdue === YES || undefined,
  };
  const query = useGetCustomerDueReport(params, {
    query: {
      queryKey: useUnitKey(getGetCustomerDueReportQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const totals = query.data?.totals;

  const money = (
    key: string,
    value: (row: CustomerDueRow) => number | null,
  ): ReportColumn<CustomerDueRow> => ({
    key,
    title: t(`customerDue.col.${key}`),
    type: 'money',
    value,
  });
  const quantity = (
    key: string,
    value: (row: CustomerDueRow) => number | null,
  ): ReportColumn<CustomerDueRow> => ({
    key,
    title: t(`customerDue.col.${key}`),
    value,
    render: (row) => formatNumber(value(row)),
  });
  const date = (
    key: string,
    value: (row: CustomerDueRow) => string | null,
  ): ReportColumn<CustomerDueRow> => ({
    key,
    title: t(`customerDue.col.${key}`),
    type: 'date',
    value,
  });
  const identity: ReportColumn<CustomerDueRow>[] =
    view === 'bill'
      ? [
          { key: 'bill', title: t('customerDue.col.bill'), value: (row) => row.code },
          { key: 'customer', title: t('customerDue.col.customer'), value: (row) => row.name },
          date('date', (row) => row.date),
          date('dueDate', (row) => row.dueDate),
          quantity('overdue', (row) => row.overdueDays),
        ]
      : [
          {
            key: 'customer',
            title: t('customerDue.col.customer'),
            value: (row) => `${row.name} (${row.code})`,
            render: (row) => <TitleCell title={row.name} subtitle={row.code} />,
          },
          quantity('bills', (row) => row.bills),
          quantity('overdue', (row) => row.overdueDays),
        ];

  const open =
    view === 'bill'
      ? (row: CustomerDueRow) => void navigate(deliveryPaths.view(row.id))
      : collections.create
        ? (row: CustomerDueRow) => void navigate(collectionPaths.fromCustomer(row.id))
        : undefined;

  return (
    <ReportPage<CustomerDueRow, Filters, DueView>
      feature={CUSTOMER_DUE_FEATURE}
      title={t('customerDue.title')}
      subtitle={t('customerDue.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      search={{ label: t('customerDue.search'), placeholder: t('customerDue.search') }}
      filters={[
        {
          key: 'customerId',
          label: t('customerDue.col.customer'),
          options: options(SALES_LOOKUPS.customer),
        },
        {
          key: 'salesForceId',
          label: t('customerDue.col.salesForce'),
          options: options(SALES_LOOKUPS.employee),
        },
        {
          key: 'overdue',
          label: t('customerDue.filter.overdue'),
          options: [{ value: YES, label: t('customerDue.filter.overdueOnly') }],
        },
      ]}
      views={CUSTOMER_DUE_VIEWS.map((key) => ({ key, label: t(`customerDue.view.${key}`) }))}
      stats={[
        {
          key: 'due',
          icon: <WalletOutlined />,
          tint: 'blue',
          label: t('customerDue.col.due'),
          value: formatMoney(totals?.due ?? 0),
        },
        {
          key: 'overdue',
          icon: <ClockCircleOutlined />,
          tint: 'amber',
          label: t('customerDue.stat.overdue'),
          value: formatMoney((totals?.due ?? 0) - (totals?.notDue ?? 0)),
        },
        {
          key: 'bills',
          icon: <FileTextOutlined />,
          tint: 'violet',
          label: t('customerDue.col.bills'),
          value: formatNumber(totals?.bills ?? 0),
        },
      ]}
      columns={[
        ...identity,
        money('billAmount', (row) => row.billAmount),
        money('returnAmount', (row) => row.returnAmount || null),
        money('received', (row) => row.received || null),
        money('due', (row) => row.due),
        money('notDue', (row) => row.notDue || null),
        money('days30', (row) => row.days30 || null),
        money('days60', (row) => row.days60 || null),
        money('days90', (row) => row.days90 || null),
        money('older', (row) => row.older || null),
        ...(view === 'customer' ? [money('creditLimit', (row) => row.creditLimit)] : []),
      ]}
      rows={query.data?.items}
      total={query.data?.total}
      rowKey={(row) => `${view}:${row.key}`}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      onRowClick={open}
      exportFile={t('customerDue.exportFile')}
      fetchAll={() =>
        getCustomerDueReport({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('report.emptyTitle'), description: t('report.emptyText') }}
    />
  );
}
