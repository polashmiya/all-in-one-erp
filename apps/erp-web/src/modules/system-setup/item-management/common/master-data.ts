import type { ItemLookup, StatusCounts } from '@erp/api-client';
import { lookupLabel } from '@erp/metadata-engine';
import type { ListSort } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { featurePath } from '@/common/constants/routes';
import { useAuthStore } from '@/common/store/auth-store';

export type StatusView = 'all' | 'active' | 'inactive';

export interface MasterRow {
  id: number;
  name: string;
  isActive: boolean;
  createdAt: string | null;
  modifiedAt: string | null;
}

export interface MasterList<T> {
  items: T[];
  total: number;
  counts: StatusCounts;
}

export const ITEM_MANAGEMENT_API = '/api/erp/item-management';

export const EXPORT_PAGE_SIZE = 1000;

export const itemManagementFeatures = {
  department: 'system-setup.item-department',
  category: 'system-setup.item-category',
  subCategory: 'system-setup.item-sub-category',
  minorCategory: 'system-setup.item-minor-category',
  uom: 'system-setup.uom',
} as const;

export const itemManagementPaths = {
  department: featurePath('system-setup', 'item-department'),
  category: featurePath('system-setup', 'item-category'),
  subCategory: featurePath('system-setup', 'item-sub-category'),
  minorCategory: featurePath('system-setup', 'item-minor-category'),
  uom: featurePath('system-setup', 'uom'),
};

interface ListQuery {
  search: string;
  view?: StatusView;
  sort?: ListSort;
  page: number;
  pageSize: number;
}

export function listParams({ search, view, sort, page, pageSize }: ListQuery) {
  return {
    search: search.trim() || undefined,
    status: view && view !== 'all' ? view : undefined,
    sort: sort ? `${sort.order === 'desc' ? '-' : ''}${sort.field}` : undefined,
    page,
    pageSize,
  };
}

export const idParam = (value: string | null | undefined) => (value ? Number(value) : undefined);

export const CODE_PATTERN = /^[A-Za-z0-9][A-Za-z0-9._-]*$/;

export const toUpper = (value: unknown) =>
  typeof value === 'string' ? value.toUpperCase() : value;

export const trimmed = (value: string | null | undefined) => {
  const text = value?.trim();
  return text ? text : null;
};

export function useUnitKey<K extends readonly unknown[]>(key: K) {
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  return [...key, unitId] as const;
}

export function useRefreshItemManagement() {
  const client = useQueryClient();
  return useCallback(
    () =>
      client.invalidateQueries({
        predicate: ({ queryKey }) =>
          typeof queryKey[0] === 'string' && queryKey[0].startsWith(ITEM_MANAGEMENT_API),
      }),
    [client],
  );
}

export { childSelectOptions, lookupLabel, parentOf, selectOptions } from '@erp/metadata-engine';

export function filterOptions(
  items: ItemLookup[] | undefined,
  inactiveLabel: (name: string) => string,
) {
  return (items ?? []).map((item) => ({
    value: String(item.id),
    label: item.isActive ? lookupLabel(item) : inactiveLabel(lookupLabel(item)),
  }));
}
