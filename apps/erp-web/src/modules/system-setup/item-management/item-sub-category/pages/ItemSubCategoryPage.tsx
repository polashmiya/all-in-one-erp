import {
  getGetItemCategoryOptionsQueryKey,
  getGetItemDepartmentOptionsQueryKey,
  getListItemSubCategoriesQueryKey,
  listItemSubCategories,
  useDeleteItemSubCategories,
  useGetItemCategoryOptions,
  useGetItemDepartmentOptions,
  useListItemSubCategories,
  useSetItemSubCategoriesStatus,
  type ItemSubCategoryRow,
} from '@erp/api-client';
import { formatNumber, SoftTag, useDisclosure, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useEffect } from 'react';
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
  parentOf,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import {
  itemManagementFeatures,
  itemManagementPaths,
  useRefreshItemManagement,
} from '../../item-management';
import { SubCategoryFormModal } from '../components/SubCategoryFormModal';

type Filters = { departmentId: string | null; categoryId: string | null };

export function ItemSubCategoryPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(itemManagementFeatures.subCategory);
  const config = useFeatureConfig(itemManagementFeatures.subCategory, access.view);
  const canOpenMinor = useFeatureAccess(itemManagementFeatures.minorCategory).view;
  const modal = useDisclosure<ItemSubCategoryRow>();
  const refresh = useRefreshItemManagement();
  const list = useListState<Filters, StatusView>({
    filters: {
      departmentId: null,
      categoryId: null,
    },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const departmentId = idParam(list.filters.departmentId);
  const categoryId = idParam(list.filters.categoryId);
  const params = { ...listParams(list.query), departmentId, categoryId };
  const query = useListItemSubCategories(params, {
    query: {
      queryKey: useUnitKey(getListItemSubCategoriesQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const departments = useGetItemDepartmentOptions({
    query: { queryKey: useUnitKey(getGetItemDepartmentOptionsQueryKey()), enabled: access.view },
  });
  const categories = useGetItemCategoryOptions(undefined, {
    query: { queryKey: useUnitKey(getGetItemCategoryOptionsQueryKey()), enabled: access.view },
  });
  const setStatus = useSetItemSubCategoriesStatus();
  const remove = useDeleteItemSubCategories();
  const { setFilter } = list;

  useEffect(() => {
    if (departmentId == null || categoryId == null || !categories.data) return;
    if (parentOf(categories.data, categoryId) !== departmentId) setFilter('categoryId', null);
  }, [departmentId, categoryId, categories.data, setFilter]);

  const edit = access.edit ? (row: ItemSubCategoryRow) => modal.onOpen(row) : undefined;
  const inactive = (name: string) => t('master.inactiveOption', { name });

  const { columns, csvColumns } = useMasterColumns<ItemSubCategoryRow>(config.data, {
    onOpen: edit,
    renderers: {
      categoryId: { render: (row) => <SoftTag>{row.categoryName}</SoftTag> },
    },
    extra: [
      {
        column: {
          key: 'minorCategories',
          title: t('subcat.col.minorCategories'),
          align: 'end',
          render: (_, row) =>
            canOpenMinor && row.minorCategories > 0 ? (
              <Link to={`${itemManagementPaths.minorCategory}?subCategoryId=${row.id}`}>
                {formatNumber(row.minorCategories)}
              </Link>
            ) : (
              formatNumber(row.minorCategories)
            ),
        },
        csv: { header: t('subcat.col.minorCategories'), value: (r) => r.minorCategories },
      },
    ],
  });

  return (
    <MasterListPage<ItemSubCategoryRow, Filters>
      feature={itemManagementFeatures.subCategory}
      title={t('subcat.title')}
      subtitle={t('subcat.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('subcat.new')}
      search={{ label: t('subcat.searchLabel'), placeholder: t('subcat.search') }}
      list={list}
      filters={[
        {
          key: 'departmentId',
          label: t('subcat.department'),
          options: filterOptions(departments.data, inactive),
        },
        {
          key: 'categoryId',
          label: t('subcat.category'),
          options: filterOptions(
            categories.data?.filter((c) => departmentId == null || c.parentId === departmentId),
            inactive,
          ),
        },
      ]}
      data={query.data}
      loading={query.isFetching || !config.data}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('subcat.exportFile')}
      fetchAll={() =>
        listItemSubCategories({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('subcat.emptyTitle'), description: t('subcat.emptyText') }}
      onCreate={() => modal.onOpen()}
      onEdit={(row) => modal.onOpen(row)}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      onDelete={(ids) => remove.mutateAsync({ data: { ids } }).then(refresh)}
    >
      {config.data && (
        <SubCategoryFormModal
          config={config.data}
          open={modal.open}
          subCategory={modal.data}
          defaults={{ departmentId, categoryId }}
          departments={departments.data}
          categories={categories.data}
          unitName={unit?.name ?? ''}
          onClose={modal.onClose}
          onSaved={refresh}
        />
      )}
    </MasterListPage>
  );
}
