import { useGetDistricts, useGetThanas } from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { appConfig } from '@/common/config/app.config';
import { featurePath } from '@/common/constants/routes';

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

export function useLocationLookups(enabled = true): LookupSources {
  const query = { enabled, staleTime: appConfig.referenceStaleMs };
  const districts = useGetDistricts({ query });
  const thanas = useGetThanas({ query });
  return { [PARTNER_LOOKUPS.district]: districts.data, [PARTNER_LOOKUPS.thana]: thanas.data };
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
