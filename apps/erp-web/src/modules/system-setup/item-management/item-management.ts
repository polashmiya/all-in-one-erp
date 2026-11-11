import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { featurePath } from '@/common/constants/routes';

export const ITEM_MANAGEMENT_API = '/api/erp/item-management';

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
