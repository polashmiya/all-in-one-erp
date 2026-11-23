import { getGetWarehouseOptionsQueryKey, useGetWarehouseOptions } from '@erp/api-client';
import type { StatusTone } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { useUnitKey } from '@/common/master-data';
import { useAuthStore } from '@/common/store/auth-store';

export const PROCUREMENT_MODULE = 'procurement';
export const PROCUREMENT_API = '/api/erp/procurement';

export const PROCUREMENT_LOOKUPS = {
  warehouse: 'warehouse',
  employee: 'employee',
  supplier: 'supplier',
  itemType: 'item-type',
} as const;

export type ApprovalStatus = 'pending' | 'approved' | 'rejected' | 'closed';
export type ApprovalView = 'all' | ApprovalStatus;

export const APPROVAL_VIEWS: ApprovalView[] = ['all', 'pending', 'approved', 'rejected', 'closed'];

export const STATUS_TONES: Record<string, StatusTone> = {
  pending: 'warning',
  approved: 'success',
  rejected: 'danger',
  closed: 'neutral',
};

export const QUANTITY_DECIMALS_SHOWN = 2;
export const REVIEW_COMMENT_MAX = 500;

export const statusParam = (view?: ApprovalView) => (view && view !== 'all' ? view : undefined);

export function pathsOf(base: string) {
  return {
    list: base,
    create: `${base}/new`,
    view: (id: number) => `${base}/${id}`,
    edit: (id: number) => `${base}/${id}/edit`,
  };
}

export type DocumentPaths = ReturnType<typeof pathsOf>;

export function useUnitWarehouses(enabled = true) {
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const warehouses = useGetWarehouseOptions({
    query: { queryKey: useUnitKey(getGetWarehouseOptionsQueryKey()), enabled },
  });
  return warehouses.data?.filter((w) => w.parentId === unitId);
}

export function useRefreshProcurement() {
  const client = useQueryClient();
  return useCallback(
    () =>
      client.invalidateQueries({
        predicate: ({ queryKey }) =>
          typeof queryKey[0] === 'string' && queryKey[0].startsWith(PROCUREMENT_API),
      }),
    [client],
  );
}
