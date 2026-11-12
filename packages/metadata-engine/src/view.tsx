import type { FeatureConfig } from '@erp/api-client';
import { DescriptionList, DetailSection, type CsvColumn, type DetailsTab } from '@erp/ui';
import type { TableColumnsType } from 'antd';
import type { ReactNode } from 'react';
import { columnsOf, fieldMap, isNumeric, useText } from './config';
import type { LookupSources } from './lookups';
import { useFieldValue } from './values';

const VIEW_COLUMNS = 3;

export function useViewTabs<T extends object>(
  config: FeatureConfig,
  record: T,
  renderers: Record<string, (record: T) => ReactNode> = {},
  lookups?: LookupSources,
): DetailsTab[] {
  const text = useText();
  const value = useFieldValue(lookups);

  return config.tabs
    .map((tab) => ({
      key: tab.key,
      label: text(tab.label),
      sections: tab.sections
        .map((section) => ({
          section,
          fields: section.fields.filter((field) => field.type !== 'image'),
        }))
        .filter(({ fields }) => fields.length > 0)
        .map(({ section, fields }) => ({
          key: section.key,
          content: (
            <DetailSection title={text(section.label)}>
              <DescriptionList
                columns={Math.max(2, Math.min(columnsOf(section.columns), VIEW_COLUMNS)) as 2 | 3}
                items={fields.map((field) => ({
                  key: field.key,
                  label: text(field.label),
                  value: renderers[field.key]?.(record) ?? value.display(field, record),
                  wide: field.type === 'textarea',
                }))}
              />
            </DetailSection>
          ),
        })),
    }))
    .filter((tab) => tab.sections.length > 0);
}

export interface ColumnRenderer<T> {
  render?: (row: T, index: number) => ReactNode;
  csv?: (row: T) => string;
}

export function useConfigColumns<T extends object>(
  config: FeatureConfig | undefined,
  renderers: Record<string, ColumnRenderer<T>> = {},
) {
  const text = useText();
  const value = useFieldValue();
  const fields = config ? fieldMap(config) : new Map();
  const shown = (config?.columns ?? []).flatMap((column) => {
    const field = fields.get(column.key);
    return field ? [{ column, field }] : [];
  });

  const columns: TableColumnsType<T> = shown.map(({ column, field }) => ({
    key: column.key,
    title: text(field.label),
    sorter: column.sortable,
    align: isNumeric(field) && field.type !== 'lookup' ? 'end' : undefined,
    render: (_: unknown, row: T, index: number) =>
      renderers[column.key]?.render?.(row, index) ?? value.display(field, row),
  }));

  const csvColumns: CsvColumn<T>[] = shown.map(({ column, field }) => ({
    header: text(field.label) ?? column.key,
    value: (row: T) => renderers[column.key]?.csv?.(row) ?? value.text(field, row),
  }));

  return { columns, csvColumns };
}
