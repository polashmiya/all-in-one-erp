import {
  getGetItemDepartmentOptionsQueryKey,
  getListItemCategoriesQueryKey,
  listItemCategories,
  useDeleteItemCategories,
  useGetItemDepartmentOptions,
  useListItemCategories,
  useSetItemCategoriesStatus,
  type ItemCategoryRow,
} from '@erp/api-client';
import { formatNumber, SoftTag, useDisclosure, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  MasterListPage,
  useMasterColumns,
  EXPORT_PAGE_SIZE,
  filterOptions,
  idParam,
  listParams,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import {
  itemManagementFeatures,
  itemManagementPaths,
  useRefreshItemManagement,
} from '../../item-management';
import { CategoryFormModal } from '../components/CategoryFormModal';

type Filters = { departmentId: string | null };

export function ItemCategoryPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(itemManagementFeatures.category);
  const config = useFeatureConfig(itemManagementFeatures.category, access.view);
  const canOpenSubCategories = useFeatureAccess(itemManagementFeatures.subCategory).view;
  const modal = useDisclosure<ItemCategoryRow>();
  const refresh = useRefreshItemManagement();
  const list = useListState<Filters, StatusView>({
    filters: { departmentId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const departmentId = idParam(list.filters.departmentId);
  const params = { ...listParams(list.query), departmentId };
  const query = useListItemCategories(params, {
    query: {
      queryKey: useUnitKey(getListItemCategoriesQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const departments = useGetItemDepartmentOptions({
    query: { queryKey: useUnitKey(getGetItemDepartmentOptionsQueryKey()), enabled: access.view },
  });
  const setStatus = useSetItemCategoriesStatus();
  const remove = useDeleteItemCategories();

  const edit = access.edit ? (row: ItemCategoryRow) => modal.onOpen(row) : undefined;

  const { columns, csvColumns } = useMasterColumns<ItemCategoryRow>(config.data, {
    onOpen: edit,
    renderers: {
      code: { render: (row) => <span className="erp-text-sm">{row.code}</span> },
      departmentId: { render: (row) => <SoftTag>{row.departmentName}</SoftTag> },
    },
    extra: [
      {
        column: {
          key: 'subCategories',
          title: t('cat.col.subCategories'),
          align: 'end',
          render: (_, row) =>
            canOpenSubCategories && row.subCategories > 0 ? (
              <Link to={`${itemManagementPaths.subCategory}?categoryId=${row.id}`}>
                {formatNumber(row.subCategories)}
              </Link>
            ) : (
              formatNumber(row.subCategories)
            ),
        },
        csv: { header: t('cat.col.subCategories'), value: (r) => r.subCategories },
      },
    ],
  });

  return (
    <MasterListPage<ItemCategoryRow, Filters>
      feature={itemManagementFeatures.category}
      title={t('cat.title')}
      subtitle={t('cat.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('cat.new')}
      search={{ label: t('cat.searchLabel'), placeholder: t('cat.search') }}
      list={list}
      filters={[
        {
          key: 'departmentId',
          label: t('cat.department'),
          options: filterOptions(departments.data, (name) => t('master.inactiveOption', { name })),
        },
      ]}
      data={query.data}
      loading={query.isFetching || !config.data}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('cat.exportFile')}
      fetchAll={() =>
        listItemCategories({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('cat.emptyTitle'), description: t('cat.emptyText') }}
      onCreate={() => modal.onOpen()}
      onEdit={(row) => modal.onOpen(row)}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      onDelete={(ids) => remove.mutateAsync({ data: { ids } }).then(refresh)}
    >
      {config.data && (
        <CategoryFormModal
          config={config.data}
          open={modal.open}
          category={modal.data}
          defaultDepartmentId={departmentId}
          departments={departments.data}
          unitName={unit?.name ?? ''}
          onClose={modal.onClose}
          onSaved={refresh}
        />
      )}
    </MasterListPage>
  );
}
