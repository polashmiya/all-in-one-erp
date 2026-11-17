import type { SaveSupplierRequest } from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { partnerRequest } from '../partner-form';
import { useLocationLookups, useLookupLists } from '../partner-management';
import { SUPPLIER_LOOKUP_LISTS } from './constants';

export function useSupplierLookups(enabled = true): LookupSources {
  const lists = useLookupLists(SUPPLIER_LOOKUP_LISTS, enabled);
  return { ...lists, ...useLocationLookups(enabled) };
}

export function toRequest(v: SaveSupplierRequest): SaveSupplierRequest {
  return { ...partnerRequest(v), tolerance: v.tolerance ?? null };
}
