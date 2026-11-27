import { DeleteOutlined, EditOutlined } from '@ant-design/icons';
import type { TableColumnsType } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { DataTable } from '../../components/table/DataTable';
import { RowActions } from '../../components/table/RowActions';
import { rowActionsWidth, type RowAction } from '../../components/table/row-actions';
import { formatDateTime } from '../../utils/format';
import type { DraftItem, ListDrafts } from './drafts';

export function DraftTable({ drafts, header }: { drafts: ListDrafts; header: ReactNode }) {
  const { t } = useTranslation();

  const actions = (draft: DraftItem): RowAction[] => [
    {
      key: 'continue',
      label: t('common.continue'),
      icon: <EditOutlined />,
      hidden: !drafts.canEdit,
      onClick: () => drafts.onContinue(draft),
    },
    {
      key: 'delete',
      label: t('common.delete'),
      icon: <DeleteOutlined />,
      danger: true,
      inMenu: true,
      hidden: !drafts.canEdit,
      confirm: {
        title: t('common.deleteDraftConfirm', { name: draft.title || t('common.untitled') }),
        danger: true,
      },
      onClick: () => drafts.onDelete(draft),
    },
  ];

  const columns: TableColumnsType<DraftItem> = [
    {
      key: 'title',
      title: t('common.draftTitle'),
      render: (_, d) => <strong>{d.title || t('common.untitled')}</strong>,
    },
    { key: 'savedBy', title: t('common.savedBy'), render: (_, d) => d.savedBy ?? '—' },
    { key: 'savedAt', title: t('common.savedAt'), render: (_, d) => formatDateTime(d.savedAt) },
    {
      key: 'actions',
      title: t('table.actions'),
      align: 'end',
      width: rowActionsWidth(drafts.rows?.[0] && actions(drafts.rows[0])),
      render: (_, d) => <RowActions actions={actions(d)} />,
    },
  ];

  return (
    <DataTable<DraftItem>
      fill
      header={header}
      rowKey="id"
      columns={columns}
      dataSource={drafts.rows}
      loading={drafts.loading}
      empty={{ title: t('common.noDrafts') }}
      onRow={(draft) => ({
        onClick: drafts.canEdit ? () => drafts.onContinue(draft) : undefined,
        className: drafts.canEdit ? 'is-clickable' : undefined,
      })}
    />
  );
}
