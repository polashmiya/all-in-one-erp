import type { FeatureConfig } from '@erp/api-client';
import { fieldMap, useText, type LookupItem } from '@erp/metadata-engine';
import { useTranslation } from 'react-i18next';
import { filterOptions } from '@/common/master-data';

export function useConfigFilters<K extends string>(
  config: FeatureConfig | undefined,
  sources: Record<K, LookupItem[] | undefined>,
) {
  const { t } = useTranslation();
  const text = useText();
  const fields = config ? fieldMap(config) : undefined;
  return (config?.filters ?? [])
    .filter((key): key is K => key in sources)
    .map((key) => ({
      key,
      label: text(fields?.get(key)?.label) ?? key,
      options: filterOptions(sources[key], (name) => t('master.inactiveOption', { name })),
    }));
}
