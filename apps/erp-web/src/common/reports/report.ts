import type { ListFilterDef } from '@erp/ui';
import { useTranslation } from 'react-i18next';

export type DateRange = { from: string | null; to: string | null };

export const NO_DATES: DateRange = { from: null, to: null };

export const dateParams = ({ from, to }: DateRange) => ({
  from: from ?? undefined,
  to: to ?? undefined,
});

export function useDateFilters<F extends DateRange>(): ListFilterDef<F>[] {
  const { t } = useTranslation();
  return [
    { key: 'from', label: t('report.from'), type: 'date' },
    { key: 'to', label: t('report.to'), type: 'date' },
  ] as ListFilterDef<F>[];
}
