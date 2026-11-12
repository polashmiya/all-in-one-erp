import type { FeatureConfig } from '@erp/api-client';
import { useConfigColumns, type ColumnRenderer } from '@erp/metadata-engine';
import type { CsvColumn } from '@erp/ui';
import type { TableColumnsType } from 'antd';
import type { MasterRow } from '../master-data';
import { RecordNameCell } from './RecordNameCell';

export interface ExtraColumn<T> {
  column: TableColumnsType<T>[number];
  csv: CsvColumn<T>;
}

export function useMasterColumns<T extends MasterRow & { imageSource?: string | null }>(
  config: FeatureConfig | undefined,
  {
    onOpen,
    renderers = {},
    extra = [],
  }: {
    onOpen?: (row: T) => void;
    renderers?: Record<string, ColumnRenderer<T>>;
    extra?: ExtraColumn<T>[];
  },
) {
  const { columns, csvColumns } = useConfigColumns<T>(config, {
    name: {
      render: (row, index) => (
        <RecordNameCell
          name={row.name}
          image={row.imageSource}
          index={index}
          onOpen={onOpen && (() => onOpen(row))}
        />
      ),
    },
    ...renderers,
  });

  return {
    columns: [...columns, ...extra.map((e) => e.column)],
    csvColumns: [...csvColumns, ...extra.map((e) => e.csv)],
  };
}
