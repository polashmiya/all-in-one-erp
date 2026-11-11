import {
  CheckCircleOutlined,
  CopyOutlined,
  DeleteOutlined,
  DownloadOutlined,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  StopOutlined,
} from '@ant-design/icons';
import {
  downloadCsv,
  ForbiddenResult,
  formatDate,
  formatDateTime,
  ListPageLayout,
  StatusTag,
  type ActionItem,
  type CsvColumn,
  type ListFilters,
  type ListFilterDef,
  type ListState,
  type RowAction,
  useUndoMessage,
} from '@erp/ui';
import { App, type TableColumnsType } from 'antd';
import { useState, type Key, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { problemMessage } from '@/common/utils/api-errors';
import type { MasterList, MasterRow, StatusView } from '../master-data';

type Override = { isActive: boolean } | 'deleted';

export interface MasterListPageProps<T extends MasterRow, F extends ListFilters> {
  feature: string;
  title: string;
  subtitle: ReactNode;
  newLabel: string;
  search: { label: string; placeholder: string };
  list: ListState<F, StatusView>;
  filters?: ListFilterDef<F>[];
  data?: MasterList<T>;
  loading: boolean;
  onRefresh: () => void;
  columns: TableColumnsType<T>;
  csvColumns: CsvColumn<T>[];
  exportFile: string;
  fetchAll: () => Promise<T[]>;
  empty: { title: string; description: string };
  onCreate: () => void;
  onEdit: (row: T) => void;
  onView?: (row: T) => void;
  onDuplicate?: (row: T) => void;
  onSetStatus: (ids: number[], isActive: boolean) => Promise<unknown>;
  onDelete: (ids: number[]) => Promise<unknown>;
  children?: ReactNode;
}

export function MasterListPage<T extends MasterRow, F extends ListFilters>({
  feature,
  title,
  subtitle,
  newLabel,
  search,
  list,
  filters,
  data,
  loading,
  onRefresh,
  columns,
  csvColumns,
  exportFile,
  fetchAll,
  empty,
  onCreate,
  onEdit,
  onView,
  onDuplicate,
  onSetStatus,
  onDelete,
  children,
}: MasterListPageProps<T, F>) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const access = useFeatureAccess(feature);
  const showUndo = useUndoMessage();
  const [overrides, setOverrides] = useState<Record<number, Override>>({});

  if (!access.view) return <ForbiddenResult />;

  const rows = data?.items
    .filter((row) => overrides[row.id] !== 'deleted')
    .map((row) => {
      const override = overrides[row.id];
      return override && override !== 'deleted' ? { ...row, ...override } : row;
    });

  const setOverride = (ids: number[], value?: Override) =>
    setOverrides((current) => {
      const next = { ...current };
      for (const id of ids) {
        if (value) next[id] = value;
        else delete next[id];
      }
      return next;
    });

  const unselect = (ids: number[]) =>
    list.setSelected(list.selected.filter((k) => !ids.includes(Number(k))));

  const changeStatus = async (ids: number[], isActive: boolean, undoable = true) => {
    const changed = ids.filter((id) => rows?.find((r) => r.id === id)?.isActive !== isActive);
    unselect(ids);
    if (!changed.length) return;
    setOverride(changed, { isActive });
    try {
      await onSetStatus(changed, isActive);
      const count = changed.length;
      const text = t(isActive ? 'master.activated' : 'master.deactivated', { count });
      if (undoable) showUndo(text, () => void changeStatus(changed, !isActive, false));
      else void message.success(text);
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    } finally {
      setOverride(changed);
    }
  };

  const remove = async (ids: number[]) => {
    setOverride(ids, 'deleted');
    unselect(ids);
    try {
      await onDelete(ids);
      void message.success(t('master.deleted', { count: ids.length }));
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    } finally {
      setOverride(ids);
    }
  };

  const exportRows = async (ids?: Key[]) => {
    try {
      const rows = await fetchAll();
      const selected = ids?.map(Number);
      downloadCsv(exportFile, selected ? rows.filter((r) => selected.includes(r.id)) : rows, [
        ...csvColumns,
        {
          header: t('master.colStatus'),
          value: (r) => (r.isActive ? t('master.active') : t('master.inactive')),
        },
        {
          header: t('master.colUpdated'),
          value: (r) => formatDateTime(r.modifiedAt ?? r.createdAt),
        },
      ]);
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  const rowActions = (row: T): RowAction[] => [
    {
      key: 'view',
      label: t('master.view'),
      icon: <EyeOutlined />,
      hidden: !onView,
      onClick: () => onView?.(row),
    },
    {
      key: 'edit',
      label: t('master.edit'),
      icon: <EditOutlined />,
      hidden: !access.edit,
      onClick: () => onEdit(row),
    },
    {
      key: 'duplicate',
      label: t('master.duplicate'),
      icon: <CopyOutlined />,
      inMenu: true,
      hidden: !onDuplicate || !access.create,
      onClick: () => onDuplicate?.(row),
    },
    {
      key: 'status',
      label: row.isActive ? t('master.deactivate') : t('master.activate'),
      icon: row.isActive ? <StopOutlined /> : <CheckCircleOutlined />,
      inMenu: true,
      hidden: !access.edit,
      onClick: () => changeStatus([row.id], !row.isActive),
    },
    {
      key: 'delete',
      label: t('master.delete'),
      icon: <DeleteOutlined />,
      inMenu: true,
      danger: true,
      hidden: !access.delete,
      confirm: {
        title: t('master.deleteOne', { name: row.name }),
        description: t('master.deleteHint'),
        okText: t('master.delete'),
        danger: true,
      },
      onClick: () => remove([row.id]),
    },
  ];

  const openRow = onView ?? (access.edit ? onEdit : undefined);

  const allColumns: TableColumnsType<T> = [
    ...columns,
    {
      key: 'status',
      title: t('master.colStatus'),
      render: (_, row) => (
        <StatusTag tone={row.isActive ? 'success' : 'neutral'} dot>
          {row.isActive ? t('master.active') : t('master.inactive')}
        </StatusTag>
      ),
    },
    {
      key: 'modifiedAt',
      title: t('master.colUpdated'),
      sorter: true,
      render: (_, row) => formatDate(row.modifiedAt ?? row.createdAt),
    },
  ];

  const newAction: ActionItem = {
    key: 'new',
    label: newLabel,
    icon: <PlusOutlined />,
    variant: 'primary',
    hidden: !access.create,
    onClick: onCreate,
  };

  return (
    <>
      <ListPageLayout<T, F, StatusView>
        title={title}
        subtitle={subtitle}
        actions={[
          {
            key: 'export',
            label: t('master.export'),
            icon: <DownloadOutlined />,
            hidden: !access.export,
            onClick: () => exportRows(),
          },
          newAction,
        ]}
        state={list}
        search={search}
        filters={filters}
        views={[
          { key: 'all', label: t('master.viewAll'), count: data?.counts.all },
          { key: 'active', label: t('master.viewActive'), count: data?.counts.active },
          { key: 'inactive', label: t('master.viewInactive'), count: data?.counts.inactive },
        ]}
        columns={allColumns}
        rowActions={onView || access.edit || access.delete ? rowActions : undefined}
        onRowClick={openRow}
        rows={rows}
        rowKey={(row) => row.id}
        total={data?.total}
        loading={loading}
        onRefresh={onRefresh}
        selectable={access.edit || access.delete || access.export}
        bulkActions={(keys) => {
          const ids = keys.map(Number);
          return [
            {
              key: 'activate',
              label: t('master.activate'),
              icon: <CheckCircleOutlined />,
              hidden: !access.edit,
              onClick: () => changeStatus(ids, true),
            },
            {
              key: 'deactivate',
              label: t('master.deactivate'),
              icon: <StopOutlined />,
              hidden: !access.edit,
              onClick: () => changeStatus(ids, false),
            },
            {
              key: 'export',
              label: t('master.export'),
              icon: <DownloadOutlined />,
              hidden: !access.export,
              onClick: () => exportRows(keys),
            },
            {
              key: 'delete',
              label: t('master.delete'),
              icon: <DeleteOutlined />,
              variant: 'danger',
              hidden: !access.delete,
              confirm: {
                title: t('master.deleteMany', { count: ids.length }),
                description: t('master.deleteHint'),
                okText: t('master.delete'),
                danger: true,
              },
              onClick: () => remove(ids),
            },
          ];
        }}
        empty={{
          title: empty.title,
          description: empty.description,
          actions: access.create ? [newAction] : undefined,
        }}
      />
      {children}
    </>
  );
}
