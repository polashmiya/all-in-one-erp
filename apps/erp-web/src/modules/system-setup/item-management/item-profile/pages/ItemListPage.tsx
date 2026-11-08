import {
  getListItemsQueryKey,
  listItems,
  useDeleteItems,
  useListItems,
  useSetItemsStatus,
  type ItemLookup,
  type ItemRow,
} from '@erp/api-client';
import { fieldMap, useConfigColumns, useText } from '@erp/metadata-engine';
import { formatMoney, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
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
  const text = useText();
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
  const config = useFeatureConfig(ITEM_PROFILE_FEATURE, access.view);
  const lookups = useItemLookups(access.view);
  const categories = lookups['item-category'];
  const setStatus = useSetItemsStatus();
  const remove = useDeleteItems();
  const { setFilter } = list;

  useEffect(() => {
    if (departmentId == null || categoryId == null || !categories) return;
    if (parentOf(categories, categoryId) !== departmentId) setFilter('categoryId', null);
  }, [departmentId, categoryId, categories, setFilter]);

  const inactive = (name: string) => t('master.inactiveOption', { name });
  const fields = config.data ? fieldMap(config.data) : undefined;
  const filterSources: Record<keyof Filters, ItemLookup[] | undefined> = {
    departmentId: lookups['item-department'],
    categoryId: categories?.filter((c) => departmentId == null || c.parentId === departmentId),
    itemTypeId: lookups['item-type'],
  };
  const filters = (config.data?.filters ?? [])
    .filter((key): key is keyof Filters => key in filterSources)
    .map((key) => ({
      key,
      label: text(fields?.get(key)?.label) ?? key,
      options: filterOptions(filterSources[key], inactive),
    }));

  const { columns, csvColumns } = useConfigColumns<ItemRow>(config.data, {
    name: {
      render: (row, index) => (
        <RecordNameCell
          name={row.name}
          image={row.imageSource}
          index={index}
          subtitle={row.barCode}
          onOpen={() => navigate(itemPaths.view(row.id))}
        />
      ),
    },
    code: { render: (row) => <span className="erp-text-sm">{row.code}</span> },
    categoryId: {
      render: (row) => (
        <TitleCell
          title={row.categoryName}
          subtitle={`${row.subCategoryName} › ${row.minorCategoryName}`}
        />
      ),
      csv: (row) => `${row.categoryName} › ${row.subCategoryName} › ${row.minorCategoryName}`,
    },
    salesPrice: { render: (row) => <strong>{formatMoney(row.salesPrice)}</strong> },
  });

  return (
    <MasterListPage<ItemRow, Filters>
      feature={ITEM_PROFILE_FEATURE}
      title={t('item.title')}
      subtitle={t('item.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('item.new')}
      search={{ label: t('item.searchLabel'), placeholder: t('item.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
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
