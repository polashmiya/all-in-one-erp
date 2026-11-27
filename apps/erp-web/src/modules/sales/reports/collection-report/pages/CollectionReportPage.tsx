import {
  BankOutlined,
  CheckCircleOutlined,
  ScissorOutlined,
  WalletOutlined,
} from '@ant-design/icons';
import {
  getCollectionReport,
  getGetCollectionReportQueryKey,
  useGetCollectionReport,
  type CollectionReportRow,
} from '@erp/api-client';
import { formatDate, formatMoney, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { dateParams, NO_DATES, useDateFilters, type DateRange } from '@/common/reports/report';
import { ReportPage, type ReportColumn } from '@/common/reports/ReportPage';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { collectionPaths } from '../../../sales/sales-collection/constants';
import { SALES_LOOKUPS } from '../../../sales-document';
import { useReportLookups } from '../../useReportLookups';
import {
  COLLECTION_REPORT_FEATURE,
  COLLECTION_REPORT_VIEWS,
  type CollectionView,
} from '../constants';

type Filters = DateRange & {
  customerId: string | null;
  paymentMethodId: string | null;
  collectedById: string | null;
};

const LISTS = [SALES_LOOKUPS.paymentMethod];

export function CollectionReportPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(COLLECTION_REPORT_FEATURE);
  const options = useReportLookups(LISTS, access.view);
  const dates = useDateFilters<Filters>();
  const list = useListState<Filters, CollectionView>({
    filters: { customerId: null, paymentMethodId: null, collectedById: null, ...NO_DATES },
    view: 'collection',
    pageSize: appConfig.defaultPageSize,
  });

  const view = list.query.view ?? 'collection';
  const { search, page, pageSize } = listParams({ ...list.query, view: undefined });
  const params = {
    view,
    search,
    page,
    pageSize,
    customerId: idParam(list.filters.customerId),
    paymentMethodId: idParam(list.filters.paymentMethodId),
    collectedById: idParam(list.filters.collectedById),
    ...dateParams(list.filters),
  };
  const query = useGetCollectionReport(params, {
    query: {
      queryKey: useUnitKey(getGetCollectionReportQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const totals = query.data?.totals;

  const money = (
    key: string,
    value: (row: CollectionReportRow) => number | null,
  ): ReportColumn<CollectionReportRow> => ({
    key,
    title: t(`collectionReport.col.${key}`),
    type: 'money',
    value,
  });
  const text = (
    key: string,
    value: (row: CollectionReportRow) => string | null,
  ): ReportColumn<CollectionReportRow> => ({
    key,
    title: t(`collectionReport.col.${key}`),
    value,
  });
  const nameOf = (row: CollectionReportRow) =>
    row.date ? formatDate(row.date) : (row.name ?? t('salesReport.none'));
  const identity: ReportColumn<CollectionReportRow>[] =
    view === 'collection'
      ? [
          {
            ...text('collection', (row) => row.code),
            render: (row) => (
              <TitleCell
                title={row.code}
                subtitle={row.isAdvance ? t('collection.advance') : undefined}
              />
            ),
          },
          {
            key: 'date',
            title: t('collectionReport.col.date'),
            type: 'date',
            value: (row) => row.date,
          },
          text('customer', (row) => row.name),
          text('method', (row) => row.method),
          text('account', (row) => row.account),
          text('instrument', (row) => row.instrument),
        ]
      : [
          text(view, nameOf),
          {
            key: 'collections',
            title: t('collectionReport.col.collections'),
            type: 'quantity',
            value: (row) => row.collections,
          },
        ];

  return (
    <ReportPage<CollectionReportRow, Filters, CollectionView>
      feature={COLLECTION_REPORT_FEATURE}
      title={t('collectionReport.title')}
      subtitle={t('collectionReport.subtitle', { unit: unit?.name ?? '' })}
      list={list}
      search={{ label: t('collectionReport.search'), placeholder: t('collectionReport.search') }}
      filters={[
        {
          key: 'customerId',
          label: t('collectionReport.col.customer'),
          options: options(SALES_LOOKUPS.customer),
        },
        {
          key: 'paymentMethodId',
          label: t('collectionReport.col.method'),
          options: options(SALES_LOOKUPS.paymentMethod),
        },
        {
          key: 'collectedById',
          label: t('collectionReport.col.collector'),
          options: options(SALES_LOOKUPS.employee),
        },
        ...dates,
      ]}
      views={COLLECTION_REPORT_VIEWS.map((key) => ({
        key,
        label: t(`collectionReport.view.${key}`),
      }))}
      stats={[
        {
          key: 'collected',
          icon: <WalletOutlined />,
          tint: 'green',
          label: t('collectionReport.col.collected'),
          value: formatMoney(totals?.collected ?? 0),
        },
        {
          key: 'advance',
          icon: <BankOutlined />,
          tint: 'blue',
          label: t('collectionReport.col.advance'),
          value: formatMoney(totals?.advance ?? 0),
        },
        {
          key: 'deductions',
          icon: <ScissorOutlined />,
          tint: 'amber',
          label: t('collectionReport.stat.deductions'),
          value: formatMoney(
            (totals?.vds ?? 0) +
              (totals?.ait ?? 0) +
              (totals?.discount ?? 0) +
              (totals?.expense ?? 0),
          ),
        },
        {
          key: 'settled',
          icon: <CheckCircleOutlined />,
          tint: 'violet',
          label: t('collectionReport.col.settled'),
          value: formatMoney(totals?.settled ?? 0),
        },
      ]}
      columns={[
        ...identity,
        money('collected', (row) => row.collected || null),
        money('advance', (row) => row.advance || null),
        money('vds', (row) => row.vds || null),
        money('ait', (row) => row.ait || null),
        money('discount', (row) => row.discount || null),
        money('expense', (row) => row.expense || null),
        money('adjusted', (row) => row.advanceAdjusted || null),
        money('settled', (row) => row.settled),
      ]}
      rows={query.data?.items}
      total={query.data?.total}
      rowKey={(row) => `${view}:${row.key}`}
      loading={query.isFetching}
      onRowClick={
        view === 'collection'
          ? (row) => row.id !== null && void navigate(collectionPaths.view(row.id))
          : undefined
      }
      exportFile={t('collectionReport.exportFile')}
      fetchAll={() =>
        getCollectionReport({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('report.emptyTitle'), description: t('report.emptyText') }}
    />
  );
}
