import { DownloadOutlined } from '@ant-design/icons';
import { problemMessage } from '@erp/api-client';
import {
  downloadCsv,
  ForbiddenResult,
  formatDate,
  formatMoney,
  formatNumber,
  ListPageLayout,
  StatGrid,
  type ListFilterDef,
  type ListFilters,
  type ListState,
  type ListViewDef,
  type StatGridItem,
} from '@erp/ui';
import { App, type TableColumnsType } from 'antd';
import type { Key, ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';

type Value = string | number | null | undefined;

export interface ReportColumn<T> {
  key: string;
  title: string;
  type?: 'money' | 'quantity' | 'date';
  value: (row: T) => Value;
  render?: (row: T) => ReactNode;
  sorter?: boolean;
}

function shown<T>({ type, value }: ReportColumn<T>, row: T): ReactNode {
  const raw = value(row);
  if (raw == null || raw === '') return '—';
  if (type === 'money') return formatMoney(raw as number);
  if (type === 'quantity') return formatNumber(raw as number, QUANTITY_DECIMALS_SHOWN);
  return type === 'date' ? formatDate(raw) : raw;
}

export interface ReportPageProps<T extends object, F extends ListFilters, V extends string> {
  feature: string;
  title: string;
  subtitle: string;
  list: ListState<F, V>;
  search?: { label: string; placeholder: string };
  filters: ListFilterDef<F>[];
  views?: ListViewDef<V>[];
  stats?: StatGridItem[];
  columns: ReportColumn<T>[];
  rows?: T[];
  total?: number;
  rowKey: (row: T) => Key;
  loading: boolean;
  onRefresh: () => void;
  onRowClick?: (row: T) => void;
  exportFile: string;
  fetchAll: () => Promise<T[]>;
  empty: { title: string; description?: string };
}

export function ReportPage<T extends object, F extends ListFilters, V extends string = string>({
  feature,
  title,
  subtitle,
  list,
  search,
  filters,
  views,
  stats,
  columns,
  rows,
  total,
  rowKey,
  loading,
  onRefresh,
  onRowClick,
  exportFile,
  fetchAll,
  empty,
}: ReportPageProps<T, F, V>) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const access = useFeatureAccess(feature);

  if (!access.view) return <ForbiddenResult />;

  const tableColumns: TableColumnsType<T> = columns.map((column) => ({
    key: column.key,
    title: column.title,
    sorter: column.sorter,
    align: column.type === 'money' || column.type === 'quantity' ? 'end' : undefined,
    render: (_, row) => column.render?.(row) ?? shown(column, row),
  }));

  const exportRows = async () => {
    try {
      downloadCsv(
        exportFile,
        await fetchAll(),
        columns.map((column) => ({ header: column.title, value: column.value })),
      );
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  return (
    <ListPageLayout<T, F, V>
      title={title}
      subtitle={subtitle}
      actions={[
        {
          key: 'export',
          label: t('master.export'),
          icon: <DownloadOutlined />,
          hidden: !access.export,
          disabled: !rows?.length,
          onClick: exportRows,
        },
      ]}
      summary={stats && <StatGrid items={stats} />}
      state={list}
      search={search}
      filters={filters}
      views={views}
      columns={tableColumns}
      rows={rows}
      rowKey={rowKey}
      total={total}
      loading={loading}
      onRefresh={onRefresh}
      onRowClick={onRowClick}
      empty={empty}
    />
  );
}
