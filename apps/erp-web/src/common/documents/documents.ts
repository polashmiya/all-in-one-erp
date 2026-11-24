import {
  getGetOfficeOptionsQueryKey,
  getGetWarehouseOptionsQueryKey,
  useGetOfficeOptions,
  useGetWarehouseOptions,
} from '@erp/api-client';
import type { StatusTone } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { useUnitKey } from '@/common/master-data';
import { useAuthStore } from '@/common/store/auth-store';

const DOCUMENT_APIS = [
  '/api/erp/procurement',
  '/api/erp/inventory',
  '/api/erp/accounting',
  '/api/erp/approvals',
];

export const DOCUMENT_LOOKUPS = {
  warehouse: 'warehouse',
  employee: 'employee',
  supplier: 'supplier',
  itemType: 'item-type',
} as const;

export type ApprovalStatus = 'pending' | 'approved' | 'rejected' | 'closed' | 'cancelled';
export type ApprovalView = 'all' | ApprovalStatus;

export const APPROVAL_VIEWS: ApprovalView[] = [
  'all',
  'pending',
  'approved',
  'rejected',
  'closed',
  'cancelled',
];

export const STATUS_TONES: Record<string, StatusTone> = {
  pending: 'warning',
  approved: 'success',
  rejected: 'danger',
  closed: 'neutral',
  cancelled: 'danger',
};

export const QUANTITY_DECIMALS_SHOWN = 2;
export const VAT_MAX = 100;

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

export function useUnitOffices(enabled = true) {
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const offices = useGetOfficeOptions({
    query: { queryKey: useUnitKey(getGetOfficeOptionsQueryKey()), enabled },
  });
  return offices.data?.filter((o) => o.parentId === unitId);
}

export function useRefreshDocuments() {
  const client = useQueryClient();
  return useCallback(
    () =>
      client.invalidateQueries({
        predicate: ({ queryKey: [key] }) =>
          typeof key === 'string' && DOCUMENT_APIS.some((api) => key.startsWith(api)),
      }),
    [client],
  );
}
