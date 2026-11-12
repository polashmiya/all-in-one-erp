import type { ConfigurationChangeRow } from '@erp/api-client';
import { AppDrawer, DataTable, formatDateTime } from '@erp/ui';
import type { TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { PROPERTY_LABELS } from '../constants';
import { changedValues, type ChangedValue } from '../history';

export function ChangeDrawer({
  change,
  title,
  onClose,
}: {
  change?: ConfigurationChangeRow;
  title: string;
  onClose: () => void;
}) {
  const { t } = useTranslation();

  const format = (value: unknown) => {
    if (value === undefined || value === null || value === '') return '—';
    if (typeof value === 'boolean') return value ? t('common.yes') : t('common.no');
    if (Array.isArray(value)) return value.join(', ');
    return String(value);
  };
  const label = (path: string[]) =>
    path
      .map((part, index) =>
        index === path.length - 1 && PROPERTY_LABELS[part] ? t(PROPERTY_LABELS[part]) : part,
      )
      .join(' › ');

  const columns: TableColumnsType<ChangedValue> = [
    { key: 'what', title: t('history.col.setting'), render: (_, row) => label(row.path) },
    { key: 'before', title: t('history.col.before'), render: (_, row) => format(row.before) },
    { key: 'after', title: t('history.col.after'), render: (_, row) => format(row.after) },
  ];

  return (
    <AppDrawer
      open={!!change}
      onClose={onClose}
      size="lg"
      title={title}
      subtitle={
        change &&
        t('history.by', {
          name: change.userName ?? '—',
          date: formatDateTime(change.at),
        })
      }
    >
      {change && (
        <DataTable<ChangedValue>
          columns={columns}
          dataSource={changedValues(change.before, change.after)}
          rowKey={(row) => row.path.join('.')}
        />
      )}
    </AppDrawer>
  );
}
