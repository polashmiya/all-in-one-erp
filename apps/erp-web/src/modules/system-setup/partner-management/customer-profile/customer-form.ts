import {
  getGetEmployeeOptionsQueryKey,
  useGetEmployeeOptions,
  type SaveCustomerRequest,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { trimmed, useUnitKey } from '@/common/master-data';
import { partnerRequest } from '../partner-form';
import { PARTNER_LOOKUPS, useLocationLookups, useLookupLists } from '../partner-management';
import { CUSTOMER_LOOKUP_LISTS } from './constants';

export function useCustomerLookups(enabled = true): LookupSources {
  const lists = useLookupLists(CUSTOMER_LOOKUP_LISTS, enabled);
  const places = useLocationLookups(enabled);
  const employees = useGetEmployeeOptions({
    query: { queryKey: useUnitKey(getGetEmployeeOptionsQueryKey()), enabled },
  });
  return { ...lists, ...places, [PARTNER_LOOKUPS.employee]: employees.data };
}

export function toRequest(v: SaveCustomerRequest): SaveCustomerRequest {
  return {
    ...partnerRequest(v),
    priceTypeId: v.priceTypeId ?? null,
    territoryId: v.territoryId ?? null,
    salesRepresentativeId: v.salesRepresentativeId ?? null,
    dateOfBirth: v.dateOfBirth ?? null,
    genderId: v.genderId ?? null,
    profession: trimmed(v.profession),
    marriageDate: v.marriageDate ?? null,
  };
}
