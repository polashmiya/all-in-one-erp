import type { ConfigField } from '@erp/api-client';
import {
  useLanguageStore,
  formatDate,
  formatDateTime,
  formatMoney,
  formatNumber,
  formatPercent,
  StatusTag,
} from '@erp/ui';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { CUSTOM_DATA, displayKey, type FieldValues } from './config';
import { localized, type LookupSources } from './lookups';

function formatted(field: ConfigField, value: unknown, symbol: boolean): string {
  const number = value as number;
  switch (field.type) {
    case 'money':
      return formatMoney(number, { symbol });
    case 'percent':
      return formatPercent(number);
    case 'number':
      return formatNumber(number);
    case 'decimal':
      return formatNumber(number, field.precision ?? 2);
    case 'date':
      return formatDate(value as string);
    case 'datetime':
      return formatDateTime(value as string);
    default:
      return String(value);
  }
}

export function useFieldValue(lookups: LookupSources = {}) {
  const { t } = useTranslation();
  const language = useLanguageStore((s) => s.language);
  const yesNo = (on: boolean) => (on ? t('common.yes') : t('common.no'));

  const valueOf = (field: ConfigField, record: object) => {
    const values = record as FieldValues;
    if (!field.custom) return values[displayKey(field)];
    const value = (values[CUSTOM_DATA] as FieldValues | null | undefined)?.[field.key];
    if (field.type !== 'lookup') return value;
    return localized(lookups[field.lookup ?? ''], language)?.find((item) => item.id === value)
      ?.name;
  };

  const display = (field: ConfigField, record: object): ReactNode => {
    const value = valueOf(field, record);
    if (field.type === 'boolean')
      return (
        <StatusTag tone={value ? 'success' : 'neutral'} dot>
          {yesNo(!!value)}
        </StatusTag>
      );
    return value == null || value === '' ? null : formatted(field, value, true);
  };

  const text = (field: ConfigField, record: object): string => {
    const value = valueOf(field, record);
    if (field.type === 'boolean') return yesNo(!!value);
    return value == null || value === '' ? '' : formatted(field, value, false);
  };

  return { display, text };
}
