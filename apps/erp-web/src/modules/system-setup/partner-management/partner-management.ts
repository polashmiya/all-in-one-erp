import {
  getGetLookupValuesQueryOptions,
  useGetDistricts,
  useGetThanas,
  type FeatureConfig,
} from '@erp/api-client';
import { fieldMap, useText, type LookupItem, type LookupSources } from '@erp/metadata-engine';
import { useQueries, useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { featurePath } from '@/common/constants/routes';
import { filterOptions } from '@/common/master-data';

export const PARTNER_MANAGEMENT_MODULE = 'system-setup';
export const PARTNER_MANAGEMENT_API = '/api/erp/partner-management';

export const CUSTOMER_FEATURE = 'system-setup.customer-profile';
export const SUPPLIER_FEATURE = 'system-setup.supplier-profile';

export const CUSTOMER_PATH = featurePath(PARTNER_MANAGEMENT_MODULE, 'customer-profile');
export const SUPPLIER_PATH = featurePath(PARTNER_MANAGEMENT_MODULE, 'supplier-profile');

export const PARTNER_ASIDE_SECTIONS = ['status', 'notes'];

export const PARTNER_LOOKUPS = {
  customerGroup: 'customer-group',
  supplierGroup: 'supplier-group',
  priceType: 'price-type',
  district: 'district',
  thana: 'thana',
  territory: 'territory',
  gender: 'gender',
  employee: 'employee',
} as const;

type Id = number | string;

const pathsOf = (base: string) => ({
  list: base,
  create: `${base}/new`,
  view: (id: Id) => `${base}/${encodeURIComponent(id)}`,
  edit: (id: Id) => `${base}/${encodeURIComponent(id)}/edit`,
});

export type PartnerPaths = ReturnType<typeof pathsOf>;

export const customerPaths = pathsOf(CUSTOMER_PATH);
export const supplierPaths = pathsOf(SUPPLIER_PATH);

export function useLookupLists(lists: readonly string[], enabled = true): LookupSources {
  const results = useQueries({
    queries: lists.map((list) => ({ ...getGetLookupValuesQueryOptions(list), enabled })),
  });
  return Object.fromEntries(lists.map((list, index) => [list, results[index]?.data]));
}

export function useLocationLookups(enabled = true): LookupSources {
  const query = { enabled, staleTime: appConfig.referenceStaleMs };
  const districts = useGetDistricts({ query });
  const thanas = useGetThanas({ query });
  return { [PARTNER_LOOKUPS.district]: districts.data, [PARTNER_LOOKUPS.thana]: thanas.data };
}

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

export function useRefreshPartners() {
  const client = useQueryClient();
  return useCallback(
    () =>
      client.invalidateQueries({
        predicate: ({ queryKey }) =>
          typeof queryKey[0] === 'string' && queryKey[0].startsWith(PARTNER_MANAGEMENT_API),
      }),
    [client],
  );
}
