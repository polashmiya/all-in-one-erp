import {
  getListItemsQueryKey,
  listItems,
  useDeleteItems,
  useListItems,
  useSetItemsStatus,
  type ItemRow,
} from '@erp/api-client';
import { formatMoney, TitleCell, useListState, type CsvColumn } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import type { TableColumnsType } from 'antd';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { MasterListPage } from '../../common/components/MasterListPage';
import { RecordNameCell } from '../../common/components/RecordNameCell';
import {
  EXPORT_PAGE_SIZE,
  filterOptions,
  idParam,
  listParams,
  parentOf,
  useRefreshItemManagement,
  useUnitKey,
  type StatusView,
} from '../../common/master-data';
import { ITEM_PROFILE_FEATURE, itemPaths } from '../constants';
import { useItemLookups } from '../item-lookups';

type Filters = {
  departmentId: string | null;
  categoryId: string | null;
  itemTypeId: string | null;
};

export function ItemListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(ITEM_PROFILE_FEATURE);
  const refresh = useRefreshItemManagement();
  const list = useListState<Filters, StatusView>({
    filters: { departmentId: null, categoryId: null, itemTypeId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const departmentId = idParam(list.filters.departmentId);
  const categoryId = idParam(list.filters.categoryId);
  const itemTypeId = idParam(list.filters.itemTypeId);
  const params = { ...listParams(list.query), departmentId, categoryId, itemTypeId };
  const query = useListItems(params, {
    query: {
      queryKey: useUnitKey(getListItemsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const lookups = useItemLookups(access.view);
  const setStatus = useSetItemsStatus();
  const remove = useDeleteItems();
  const { setFilter } = list;

  useEffect(() => {
    if (departmentId == null || categoryId == null || !lookups.categories) return;
    if (parentOf(lookups.categories, categoryId) !== departmentId) setFilter('categoryId', null);
  }, [departmentId, categoryId, lookups.categories, setFilter]);

  const inactive = (name: string) => t('master.inactiveOption', { name });
  const visibleCategories = lookups.categories?.filter(
    (c) => departmentId == null || c.parentId === departmentId,
  );

  const columns: TableColumnsType<ItemRow> = [
    {
      key: 'name',
      title: t('item.col.item'),
      sorter: true,
      render: (_, row, index) => (
        <RecordNameCell
          name={row.name}
          image={row.imageSource}
          index={index}
          subtitle={row.barCode}
          onOpen={() => navigate(itemPaths.view(row.id))}
        />
      ),
    },
    {
      key: 'code',
      title: t('item.col.code'),
      sorter: true,
      render: (_, row) => <span className="erp-text-sm">{row.code}</span>,
    },
    {
      key: 'category',
      title: t('item.col.category'),
      render: (_, row) => (
        <TitleCell
          title={row.categoryName}
          subtitle={`${row.subCategoryName} › ${row.minorCategoryName}`}
        />
      ),
    },
    { key: 'type', title: t('item.col.type'), render: (_, row) => row.itemTypeName },
    { key: 'unit', title: t('item.col.unit'), render: (_, row) => row.uomName },
    {
      key: 'salesPrice',
      title: t('item.col.salesPrice'),
      align: 'end',
      sorter: true,
      render: (_, row) => <strong>{formatMoney(row.salesPrice)}</strong>,
    },
  ];

  const csvColumns: CsvColumn<ItemRow>[] = [
    { header: t('item.col.code'), value: (r) => r.code },
    { header: t('item.col.item'), value: (r) => r.name },
    { header: t('item.col.barcode'), value: (r) => r.barCode },
    { header: t('item.col.type'), value: (r) => r.itemTypeName },
    { header: t('item.col.department'), value: (r) => r.departmentName },
    { header: t('item.col.category'), value: (r) => r.categoryName },
    { header: t('item.col.subCategory'), value: (r) => r.subCategoryName },
    { header: t('item.col.minorCategory'), value: (r) => r.minorCategoryName },
    { header: t('item.col.unit'), value: (r) => r.uomName },
    {
      header: t('item.col.salesPrice'),
      value: (r) => formatMoney(r.salesPrice, { symbol: false }),
    },
  ];

  return (
    <MasterListPage<ItemRow, Filters>
      feature={ITEM_PROFILE_FEATURE}
      title={t('item.title')}
      subtitle={t('item.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('item.new')}
      search={{ label: t('item.searchLabel'), placeholder: t('item.search') }}
      list={list}
      filters={[
        {
          key: 'departmentId',
          label: t('item.department'),
          options: filterOptions(lookups.departments, inactive),
        },
        {
          key: 'categoryId',
          label: t('item.category'),
          options: filterOptions(visibleCategories, inactive),
        },
        {
          key: 'itemTypeId',
          label: t('item.itemType'),
          options: filterOptions(lookups.itemTypes, inactive),
        },
      ]}
      data={query.data}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('item.exportFile')}
      fetchAll={() =>
        listItems({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('item.emptyTitle'), description: t('item.emptyText') }}
      onCreate={() => navigate(itemPaths.create)}
      onEdit={(row) => navigate(itemPaths.edit(row.id))}
      onView={(row) => navigate(itemPaths.view(row.id))}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      onDelete={(ids) => remove.mutateAsync({ data: { ids } }).then(refresh)}
    />
  );
}
