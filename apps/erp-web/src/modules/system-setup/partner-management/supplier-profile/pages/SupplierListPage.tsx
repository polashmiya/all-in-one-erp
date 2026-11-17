import {
  getListSuppliersQueryKey,
  listSuppliers,
  useListSuppliers,
  useSetSuppliersStatus,
  type SupplierRow,
} from '@erp/api-client';
import { useConfigColumns } from '@erp/metadata-engine';
import { useListState, withDraft } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  EXPORT_PAGE_SIZE,
  idParam,
  listParams,
  MasterListPage,
  RecordNameCell,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import {
  PARTNER_LOOKUPS,
  SUPPLIER_FEATURE,
  supplierPaths,
  useConfigFilters,
  useRefreshPartners,
} from '../../partner-management';
import { useSupplierLookups } from '../supplier-form';

type Filters = {
  groupId: string | null;
  districtId: string | null;
};

export function SupplierListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(SUPPLIER_FEATURE);
  const refresh = useRefreshPartners();
  const list = useListState<Filters, StatusView>({
    filters: { groupId: null, districtId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams(list.query),
    groupId: idParam(list.filters.groupId),
    districtId: idParam(list.filters.districtId),
  };
  const query = useListSuppliers(params, {
    query: {
      queryKey: useUnitKey(getListSuppliersQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(SUPPLIER_FEATURE, access.view);
  const lookups = useSupplierLookups(access.view);
  const setStatus = useSetSuppliersStatus();

  const filters = useConfigFilters<keyof Filters>(config.data, {
    groupId: lookups[PARTNER_LOOKUPS.supplierGroup],
    districtId: lookups[PARTNER_LOOKUPS.district],
  });

  const { columns, csvColumns } = useConfigColumns<SupplierRow>(config.data, {
    name: {
      render: (row, index) => (
        <RecordNameCell
          name={row.name}
          index={index}
          subtitle={row.billingName}
          onOpen={() => navigate(supplierPaths.view(row.id))}
        />
      ),
    },
  });

  return (
    <MasterListPage<SupplierRow, Filters>
      feature={SUPPLIER_FEATURE}
      title={t('supplier.title')}
      subtitle={t('supplier.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('supplier.new')}
      search={{ label: t('supplier.searchLabel'), placeholder: t('supplier.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('supplier.exportFile')}
      fetchAll={() =>
        listSuppliers({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('supplier.emptyTitle'), description: t('supplier.emptyText') }}
      onCreate={(draftId) => navigate(withDraft(supplierPaths.create, draftId))}
      onEdit={(row) => navigate(supplierPaths.edit(row.id))}
      onView={(row) => navigate(supplierPaths.view(row.id))}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
    />
  );
}
