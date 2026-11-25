import {
  getListSalesQuotationsQueryKey,
  listSalesQuotations,
  useListSalesQuotations,
  type SalesQuotationRow,
} from '@erp/api-client';
import { formatDate, StatusTag, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { Space } from 'antd';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { DocumentListPage } from '@/common/documents/DocumentListPage';
import { statusParam, type ApprovalView } from '@/common/documents/documents';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { QUOTATION_FEATURE, QUOTATION_LOOKUPS, quotationPaths } from '../constants';
import { quotationCalls, useQuotationLookups } from '../quotation-form';

type Filters = {
  customerId: string | null;
  quotationTypeId: string | null;
  officeId: string | null;
  salesForceId: string | null;
};

export function QuotationListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(QUOTATION_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { customerId: null, quotationTypeId: null, officeId: null, salesForceId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    customerId: idParam(list.filters.customerId),
    quotationTypeId: idParam(list.filters.quotationTypeId),
    officeId: list.filters.officeId ?? undefined,
    salesForceId: idParam(list.filters.salesForceId),
  };
  const query = useListSalesQuotations(params, {
    query: {
      queryKey: useUnitKey(getListSalesQuotationsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(QUOTATION_FEATURE, access.view);
  const lookups = useQuotationLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    customerId: lookups[QUOTATION_LOOKUPS.customer],
    quotationTypeId: lookups[QUOTATION_LOOKUPS.quotationType],
    officeId: lookups[QUOTATION_LOOKUPS.office],
    salesForceId: lookups[QUOTATION_LOOKUPS.employee],
  });

  return (
    <DocumentListPage<SalesQuotationRow, Filters>
      feature={QUOTATION_FEATURE}
      config={config.data}
      title={t('quotation.title')}
      subtitle={t('quotation.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('quotation.new')}
      search={{ label: t('quotation.searchLabel'), placeholder: t('quotation.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('quotation.totals.total'), value: (row) => row.totalAmount }}
      renderers={{
        validityDate: {
          render: (row) => (
            <Space>
              {formatDate(row.validityDate)}
              {row.isExpired && <StatusTag tone="danger">{t('quotation.expired')}</StatusTag>}
            </Space>
          ),
        },
      }}
      lines={{
        label: t('document.col.items'),
        value: (row) => row.itemCount,
        subtitle: (row) =>
          [row.version, t('document.itemCount', { count: row.itemCount })].join(' · '),
      }}
      exportFile={t('quotation.exportFile')}
      fetchAll={() =>
        listSalesQuotations({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('quotation.emptyTitle'), description: t('quotation.emptyText') }}
      paths={quotationPaths}
      calls={quotationCalls}
    />
  );
}
