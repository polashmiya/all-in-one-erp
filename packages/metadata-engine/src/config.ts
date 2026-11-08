import type { ConfigField, FeatureConfig, LocalizedLabel } from '@erp/api-client';
import { localize, useLanguageStore } from '@erp/ui';
import { useCallback } from 'react';

export type FieldValues = Record<string, unknown>;

const NUMERIC_TYPES = new Set(['number', 'decimal', 'money', 'percent', 'lookup']);

export const isNumeric = (field: ConfigField) => NUMERIC_TYPES.has(field.type);

export const isText = (field: ConfigField) => field.type === 'text' || field.type === 'textarea';

export const fieldsOf = (config: FeatureConfig) =>
  config.tabs.flatMap((tab) => tab.sections.flatMap((section) => section.fields));

export const fieldMap = (config: FeatureConfig) =>
  new Map(fieldsOf(config).map((field) => [field.key, field]));

export const displayKey = (field: ConfigField) =>
  field.type === 'lookup' && field.key.endsWith('Id') ? `${field.key.slice(0, -2)}Name` : field.key;

export const columnsOf = (count: number) => Math.min(Math.max(count, 1), 4) as 1 | 2 | 3 | 4;

export function defaultValues(config: FeatureConfig): FieldValues {
  const values: FieldValues = {};
  for (const field of fieldsOf(config)) {
    if (field.type === 'boolean') values[field.key] = field.defaultValue === 'true';
    else if (field.defaultValue != null)
      values[field.key] = isNumeric(field) ? Number(field.defaultValue) : field.defaultValue;
  }
  return values;
}

export function useText() {
  const language = useLanguageStore((s) => s.language);
  return useCallback(
    (label: LocalizedLabel | null | undefined) =>
      label
        ? localize({ en: label.en, ...(label.bn ? { bn: label.bn } : {}) }, language)
        : undefined,
    [language],
  );
}
