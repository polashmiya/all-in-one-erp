import type { ConfigField, FeatureConfig, LocalizedLabel } from '@erp/api-client';
import { localize, useLanguageStore } from '@erp/ui';
import { useCallback } from 'react';

export type FieldValues = Record<string, unknown>;

const NUMERIC_TYPES = new Set(['number', 'decimal', 'money', 'percent', 'lookup']);

export const isNumeric = (field: ConfigField) => NUMERIC_TYPES.has(field.type);

export const isText = (field: ConfigField) => field.type === 'text' || field.type === 'textarea';

export const CUSTOM_DATA = 'customData';

export const namePath = (field: ConfigField) =>
  field.custom ? [CUSTOM_DATA, field.key] : field.key;

export const fieldsOf = (config: FeatureConfig) =>
  config.tabs.flatMap((tab) => tab.sections.flatMap((section) => section.fields));

export const fieldMap = (config: FeatureConfig) =>
  new Map(fieldsOf(config).map((field) => [field.key, field]));

export const displayKey = (field: ConfigField) =>
  field.type === 'lookup' && field.key.endsWith('Id') ? `${field.key.slice(0, -2)}Name` : field.key;

export const columnsOf = (count: number) => Math.min(Math.max(count, 1), 4) as 1 | 2 | 3 | 4;

export function defaultValues(config: FeatureConfig): FieldValues {
  const values: FieldValues = {};
  const custom: FieldValues = {};
  for (const field of fieldsOf(config)) {
    const target = field.custom ? custom : values;
    if (field.type === 'boolean') target[field.key] = field.defaultValue === 'true';
    else if (field.defaultValue != null)
      target[field.key] = isNumeric(field) ? Number(field.defaultValue) : field.defaultValue;
  }
  if (Object.keys(custom).length > 0) values[CUSTOM_DATA] = custom;
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
