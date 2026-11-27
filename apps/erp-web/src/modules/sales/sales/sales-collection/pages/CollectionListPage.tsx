import {
  getListSalesCollectionsQueryKey,
  listSalesCollections,
  useListSalesCollections,
  type SalesCollectionRow,
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
import { collectionCalls, useCollectionLookups } from '../collection-form';
import { COLLECTION_FEATURE, COLLECTION_LOOKUPS, collectionPaths } from '../constants';

type Filters = {
  customerId: string | null;
  paymentMethodId: string | null;
  collectedById: string | null;
};

export function CollectionListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(COLLECTION_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { customerId: null, paymentMethodId: null, collectedById: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    customerId: idParam(list.filters.customerId),
    paymentMethodId: idParam(list.filters.paymentMethodId),
    collectedById: idParam(list.filters.collectedById),
  };
  const query = useListSalesCollections(params, {
    query: {
      queryKey: useUnitKey(getListSalesCollectionsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(COLLECTION_FEATURE, access.view);
  const { lookups } = useCollectionLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    customerId: lookups[COLLECTION_LOOKUPS.customer],
    paymentMethodId: lookups[COLLECTION_LOOKUPS.paymentMethod],
    collectedById: lookups[COLLECTION_LOOKUPS.employee],
  });

  return (
    <DocumentListPage<SalesCollectionRow, Filters>
      feature={COLLECTION_FEATURE}
      config={config.data}
      title={t('collection.title')}
      subtitle={t('collection.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('collection.new')}
      search={{ label: t('collection.searchLabel'), placeholder: t('collection.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      amount={{ label: t('collection.amount'), value: (row) => row.collectionAmount }}
      lines={{
        label: t('collection.bills'),
        value: (row) => row.billCount,
        subtitle: (row) =>
          row.isAdvance
            ? t('collection.advance')
            : t('collection.billCount', { count: row.billCount }),
      }}
      exportFile={t('collection.exportFile')}
      fetchAll={() =>
        listSalesCollections({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('collection.emptyTitle'), description: t('collection.emptyText') }}
      paths={collectionPaths}
      calls={collectionCalls}
    />
  );
}
