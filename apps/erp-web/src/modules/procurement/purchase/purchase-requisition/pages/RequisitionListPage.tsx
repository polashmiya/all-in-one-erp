import {
  getListPurchaseRequisitionsQueryKey,
  listPurchaseRequisitions,
  useListPurchaseRequisitions,
  type RequisitionRow,
} from '@erp/api-client';
import { useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { DocumentListPage } from '@/common/documents/DocumentListPage';
import { statusParam, type ApprovalView } from '@/common/documents/documents';
import { REQUISITION_FEATURE, REQUISITION_LOOKUPS, requisitionPaths } from '../constants';
import { requisitionCalls, useRequisitionLookups } from '../requisition-form';

type Filters = {
  warehouseId: string | null;
  requestTypeId: string | null;
  departmentId: string | null;
};

export function RequisitionListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(REQUISITION_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { warehouseId: null, requestTypeId: null, departmentId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    warehouseId: list.filters.warehouseId ?? undefined,
    requestTypeId: idParam(list.filters.requestTypeId),
    departmentId: idParam(list.filters.departmentId),
  };
  const query = useListPurchaseRequisitions(params, {
    query: {
      queryKey: useUnitKey(getListPurchaseRequisitionsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(REQUISITION_FEATURE, access.view);
  const lookups = useRequisitionLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    warehouseId: lookups[REQUISITION_LOOKUPS.warehouse],
    requestTypeId: lookups[REQUISITION_LOOKUPS.requestType],
    departmentId: lookups[REQUISITION_LOOKUPS.department],
  });

  return (
    <DocumentListPage<RequisitionRow, Filters>
      feature={REQUISITION_FEATURE}
      config={config.data}
      title={t('requisition.title')}
      subtitle={t('requisition.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('requisition.new')}
      search={{ label: t('requisition.searchLabel'), placeholder: t('requisition.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('requisition.col.amount'), value: (row) => row.totalAmount }}
      exportFile={t('requisition.exportFile')}
      fetchAll={() =>
        listPurchaseRequisitions({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('requisition.emptyTitle'), description: t('requisition.emptyText') }}
      paths={requisitionPaths}
      calls={requisitionCalls}
    />
  );
}
