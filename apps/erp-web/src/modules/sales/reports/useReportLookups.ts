import { useTranslation } from 'react-i18next';
import { filterOptions } from '@/common/master-data';
import { useSalesLookups } from '../sales-document';

export function useReportLookups(lists: readonly string[], enabled: boolean) {
  const { t } = useTranslation();
  const lookups = useSalesLookups(lists, enabled);
  return (key: string) =>
    filterOptions(lookups[key], (name) => t('master.inactiveOption', { name }));
}
