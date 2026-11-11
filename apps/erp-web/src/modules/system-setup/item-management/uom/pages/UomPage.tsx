import {
  getListUomsQueryKey,
  listUoms,
  useDeleteUoms,
  useListUoms,
  useSetUomsStatus,
  type UomRow,
} from '@erp/api-client';
import { SoftTag, useDisclosure, useListState, type CsvColumn } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import type { TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useAuthStore } from '@/common/store/auth-store';
import {
  MasterListPage,
  RecordNameCell,
  EXPORT_PAGE_SIZE,
  listParams,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import { itemManagementFeatures, useRefreshItemManagement } from '../../item-management';
import { UomFormModal } from '../components/UomFormModal';

type Filters = { decimalAllowed: string | null };
type TFunction = ReturnType<typeof useTranslation>['t'];

const quantityLabel = (t: TFunction, row: UomRow) =>
  row.isDecimalAllowed
    ? t('uom.decimalPlaces', { count: row.roundingLength ?? 0 })
    : t('uom.whole');

export function UomPage() {
  const { t } = useTranslation();
  const accountName = useAuthStore((s) => s.user?.accountName ?? '');
  const access = useFeatureAccess(itemManagementFeatures.uom);
  const modal = useDisclosure<UomRow>();
  const refresh = useRefreshItemManagement();
  const list = useListState<Filters, StatusView>({
    filters: { decimalAllowed: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const decimal = list.filters.decimalAllowed;
  const params = {
    ...listParams(list.query),
    decimalAllowed: decimal === null ? undefined : decimal === 'true',
  };
  const query = useListUoms(params, {
    query: {
      queryKey: useUnitKey(getListUomsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const setStatus = useSetUomsStatus();
  const remove = useDeleteUoms();

  const edit = access.edit ? (row: UomRow) => modal.onOpen(row) : undefined;

  const columns: TableColumnsType<UomRow> = [
    {
      key: 'name',
      title: t('uom.col.unit'),
      sorter: true,
      render: (_, row, index) => (
        <RecordNameCell name={row.name} index={index} onOpen={edit && (() => edit(row))} />
      ),
    },
    {
      key: 'code',
      title: t('uom.col.code'),
      dataIndex: 'code',
      sorter: true,
      render: (code: string) => <SoftTag>{code}</SoftTag>,
    },
    {
      key: 'quantity',
      title: t('uom.col.quantity'),
      render: (_, row) => quantityLabel(t, row),
    },
    {
      key: 'description',
      title: t('uom.col.description'),
      ellipsis: true,
      render: (_, row) => row.description ?? '—',
    },
  ];

  const csvColumns: CsvColumn<UomRow>[] = [
    { header: t('uom.col.code'), value: (r) => r.code },
    { header: t('uom.col.unit'), value: (r) => r.name },
    { header: t('uom.col.quantity'), value: (r) => quantityLabel(t, r) },
    { header: t('uom.col.description'), value: (r) => r.description },
  ];

  return (
    <MasterListPage<UomRow, Filters>
      feature={itemManagementFeatures.uom}
      title={t('uom.title')}
      subtitle={t('uom.subtitle', { account: accountName })}
      newLabel={t('uom.new')}
      search={{ label: t('uom.searchLabel'), placeholder: t('uom.search') }}
      list={list}
      filters={[
        {
          key: 'decimalAllowed',
          label: t('uom.quantity'),
          options: [
            { value: 'true', label: t('uom.decimal') },
            { value: 'false', label: t('uom.whole') },
          ],
        },
      ]}
      data={query.data}
      loading={query.isFetching}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('uom.exportFile')}
      fetchAll={() =>
        listUoms({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('uom.emptyTitle'), description: t('uom.emptyText') }}
      onCreate={() => modal.onOpen()}
      onEdit={(row) => modal.onOpen(row)}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      onDelete={(ids) => remove.mutateAsync({ data: { ids } }).then(refresh)}
    >
      <UomFormModal
        open={modal.open}
        uom={modal.data}
        accountName={accountName}
        onClose={modal.onClose}
        onSaved={refresh}
      />
    </MasterListPage>
  );
}
