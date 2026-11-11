import {
  getGetItemCategoryOptionsQueryKey,
  getGetItemDepartmentOptionsQueryKey,
  getGetItemSubCategoryOptionsQueryKey,
  getListItemMinorCategoriesQueryKey,
  listItemMinorCategories,
  useDeleteItemMinorCategories,
  useGetItemCategoryOptions,
  useGetItemDepartmentOptions,
  useGetItemSubCategoryOptions,
  useListItemMinorCategories,
  useSetItemMinorCategoriesStatus,
  type ItemMinorCategoryRow,
} from '@erp/api-client';
import { SoftTag, useDisclosure, useListState, type CsvColumn } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import type { TableColumnsType } from 'antd';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  MasterListPage,
  RecordNameCell,
  EXPORT_PAGE_SIZE,
  filterOptions,
  idParam,
  listParams,
  parentOf,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import { itemManagementFeatures, useRefreshItemManagement } from '../../item-management';
import { MinorCategoryFormModal } from '../components/MinorCategoryFormModal';

type Filters = {
  departmentId: string | null;
  categoryId: string | null;
  subCategoryId: string | null;
};

export function ItemMinorCategoryPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(itemManagementFeatures.minorCategory);
  const modal = useDisclosure<ItemMinorCategoryRow>();
  const refresh = useRefreshItemManagement();
  const list = useListState<Filters, StatusView>({
    filters: {
      departmentId: null,
      categoryId: null,
      subCategoryId: null,
    },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const departmentId = idParam(list.filters.departmentId);
  const categoryId = idParam(list.filters.categoryId);
  const subCategoryId = idParam(list.filters.subCategoryId);
  const params = { ...listParams(list.query), departmentId, categoryId, subCategoryId };
  const query = useListItemMinorCategories(params, {
    query: {
      queryKey: useUnitKey(getListItemMinorCategoriesQueryKey(params)),
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
  const subCategories = useGetItemSubCategoryOptions(undefined, {
    query: { queryKey: useUnitKey(getGetItemSubCategoryOptionsQueryKey()), enabled: access.view },
  });
  const setStatus = useSetItemMinorCategoriesStatus();
  const remove = useDeleteItemMinorCategories();
  const { setFilter } = list;

  useEffect(() => {
    if (departmentId == null || categoryId == null || !categories.data) return;
    if (parentOf(categories.data, categoryId) !== departmentId) setFilter('categoryId', null);
  }, [departmentId, categoryId, categories.data, setFilter]);

  useEffect(() => {
    if (subCategoryId == null || !subCategories.data || !categories.data) return;
    const parentCategory = parentOf(subCategories.data, subCategoryId);
    const outsideCategory = categoryId != null && parentCategory !== categoryId;
    const outsideDepartment =
      departmentId != null && parentOf(categories.data, parentCategory) !== departmentId;
    if (outsideCategory || outsideDepartment) setFilter('subCategoryId', null);
  }, [departmentId, categoryId, subCategoryId, categories.data, subCategories.data, setFilter]);

  const edit = access.edit ? (row: ItemMinorCategoryRow) => modal.onOpen(row) : undefined;
  const inactive = (name: string) => t('master.inactiveOption', { name });
  const visibleCategories = categories.data?.filter(
    (c) => departmentId == null || c.parentId === departmentId,
  );
  const visibleSubCategories = subCategories.data?.filter((s) =>
    categoryId != null
      ? s.parentId === categoryId
      : departmentId == null || visibleCategories?.some((c) => c.id === s.parentId),
  );

  const columns: TableColumnsType<ItemMinorCategoryRow> = [
    {
      key: 'name',
      title: t('minor.col.minorCategory'),
      sorter: true,
      render: (_, row, index) => (
        <RecordNameCell
          name={row.name}
          image={row.imageSource}
          index={index}
          onOpen={edit && (() => edit(row))}
        />
      ),
    },
    {
      key: 'subCategory',
      title: t('minor.col.subCategory'),
      sorter: true,
      render: (_, row) => <SoftTag>{row.subCategoryName}</SoftTag>,
    },
    {
      key: 'category',
      title: t('minor.col.category'),
      sorter: true,
      render: (_, row) => row.categoryName,
    },
    {
      key: 'department',
      title: t('minor.col.department'),
      render: (_, row) => row.departmentName ?? '—',
    },
  ];

  const csvColumns: CsvColumn<ItemMinorCategoryRow>[] = [
    { header: t('minor.col.minorCategory'), value: (r) => r.name },
    { header: t('minor.col.subCategory'), value: (r) => r.subCategoryName },
    { header: t('minor.col.category'), value: (r) => r.categoryName },
    { header: t('minor.col.department'), value: (r) => r.departmentName },
  ];

  return (
    <MasterListPage<ItemMinorCategoryRow, Filters>
      feature={itemManagementFeatures.minorCategory}
      title={t('minor.title')}
      subtitle={t('minor.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('minor.new')}
      search={{ label: t('minor.searchLabel'), placeholder: t('minor.search') }}
      list={list}
      filters={[
        {
          key: 'departmentId',
          label: t('minor.department'),
          options: filterOptions(departments.data, inactive),
        },
        {
          key: 'categoryId',
          label: t('minor.category'),
          options: filterOptions(visibleCategories, inactive),
        },
        {
          key: 'subCategoryId',
          label: t('minor.subCategory'),
          options: filterOptions(visibleSubCategories, inactive),
        },
      ]}
      data={query.data}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('minor.exportFile')}
      fetchAll={() =>
        listItemMinorCategories({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('minor.emptyTitle'), description: t('minor.emptyText') }}
      onCreate={() => modal.onOpen()}
      onEdit={(row) => modal.onOpen(row)}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      onDelete={(ids) => remove.mutateAsync({ data: { ids } }).then(refresh)}
    >
      <MinorCategoryFormModal
        open={modal.open}
        minorCategory={modal.data}
        defaults={{ departmentId, categoryId, subCategoryId }}
        departments={departments.data}
        categories={categories.data}
        subCategories={subCategories.data}
        unitName={unit?.name ?? ''}
        onClose={modal.onClose}
        onSaved={refresh}
      />
    </MasterListPage>
  );
}
