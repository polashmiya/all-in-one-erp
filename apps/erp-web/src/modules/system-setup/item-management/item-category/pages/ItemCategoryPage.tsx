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
import { formatNumber, SoftTag, useDisclosure, useListState, type CsvColumn } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import type { TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
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

  const columns: TableColumnsType<ItemCategoryRow> = [
    {
      key: 'name',
      title: t('cat.col.category'),
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
      key: 'code',
      title: t('cat.col.code'),
      dataIndex: 'code',
      sorter: true,
      render: (code: string) => <span className="erp-text-sm">{code}</span>,
    },
    {
      key: 'department',
      title: t('cat.col.department'),
      sorter: true,
      render: (_, row) => <SoftTag>{row.departmentName}</SoftTag>,
    },
    {
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
  ];

  const csvColumns: CsvColumn<ItemCategoryRow>[] = [
    { header: t('cat.col.code'), value: (r) => r.code },
    { header: t('cat.col.category'), value: (r) => r.name },
    { header: t('cat.col.department'), value: (r) => r.departmentName },
    { header: t('cat.col.subCategories'), value: (r) => r.subCategories },
  ];

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
      loading={query.isFetching}
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
      <CategoryFormModal
        open={modal.open}
        category={modal.data}
        defaultDepartmentId={departmentId}
        departments={departments.data}
        unitName={unit?.name ?? ''}
        onClose={modal.onClose}
        onSaved={refresh}
      />
    </MasterListPage>
  );
}
