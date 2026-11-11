import {
  getListItemDepartmentsQueryKey,
  listItemDepartments,
  useDeleteItemDepartments,
  useListItemDepartments,
  useSetItemDepartmentsStatus,
  type ItemDepartmentRow,
} from '@erp/api-client';
import { formatNumber, useDisclosure, useListState, type CsvColumn } from '@erp/ui';
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
  listParams,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import {
  itemManagementFeatures,
  itemManagementPaths,
  useRefreshItemManagement,
} from '../../item-management';
import { DepartmentFormModal } from '../components/DepartmentFormModal';

type Filters = Record<string, never>;

export function ItemDepartmentPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(itemManagementFeatures.department);
  const canOpenCategories = useFeatureAccess(itemManagementFeatures.category).view;
  const modal = useDisclosure<ItemDepartmentRow>();
  const refresh = useRefreshItemManagement();
  const list = useListState<Filters, StatusView>({
    filters: {},
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = listParams(list.query);
  const query = useListItemDepartments(params, {
    query: {
      queryKey: useUnitKey(getListItemDepartmentsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const setStatus = useSetItemDepartmentsStatus();
  const remove = useDeleteItemDepartments();

  const edit = access.edit ? (row: ItemDepartmentRow) => modal.onOpen(row) : undefined;

  const columns: TableColumnsType<ItemDepartmentRow> = [
    {
      key: 'name',
      title: t('dept.col.department'),
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
      title: t('dept.col.code'),
      dataIndex: 'code',
      sorter: true,
      render: (code: string) => <span className="erp-text-sm">{code}</span>,
    },
    {
      key: 'serial',
      title: t('dept.col.serial'),
      dataIndex: 'serial',
      sorter: true,
      align: 'end',
      render: (serial: number | null) => serial ?? '—',
    },
    {
      key: 'categories',
      title: t('dept.col.categories'),
      align: 'end',
      render: (_, row) =>
        canOpenCategories && row.categories > 0 ? (
          <Link to={`${itemManagementPaths.category}?departmentId=${row.id}`}>
            {formatNumber(row.categories)}
          </Link>
        ) : (
          formatNumber(row.categories)
        ),
    },
  ];

  const csvColumns: CsvColumn<ItemDepartmentRow>[] = [
    { header: t('dept.col.code'), value: (r) => r.code },
    { header: t('dept.col.department'), value: (r) => r.name },
    { header: t('dept.col.serial'), value: (r) => r.serial },
    { header: t('dept.col.categories'), value: (r) => r.categories },
  ];

  return (
    <MasterListPage<ItemDepartmentRow, Filters>
      feature={itemManagementFeatures.department}
      title={t('dept.title')}
      subtitle={t('dept.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('dept.new')}
      search={{ label: t('dept.searchLabel'), placeholder: t('dept.search') }}
      list={list}
      data={query.data}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('dept.exportFile')}
      fetchAll={() =>
        listItemDepartments({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('dept.emptyTitle'), description: t('dept.emptyText') }}
      onCreate={() => modal.onOpen()}
      onEdit={(row) => modal.onOpen(row)}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      onDelete={(ids) => remove.mutateAsync({ data: { ids } }).then(refresh)}
    >
      <DepartmentFormModal
        open={modal.open}
        department={modal.data}
        unitName={unit?.name ?? ''}
        onClose={modal.onClose}
        onSaved={refresh}
      />
    </MasterListPage>
  );
}
