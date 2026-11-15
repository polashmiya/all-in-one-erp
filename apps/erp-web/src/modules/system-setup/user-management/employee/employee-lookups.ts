import {
  getGetEmployeeOptionsQueryKey,
  getGetLookupValuesQueryOptions,
  getGetOfficeOptionsQueryKey,
  getGetWarehouseOptionsQueryKey,
  useGetEmployeeOptions,
  useGetOfficeOptions,
  useGetWarehouseOptions,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { useQueries } from '@tanstack/react-query';
import { useAuthStore } from '@/common/store/auth-store';
import { useUnitKey } from '@/common/master-data';
import { EMPLOYEE_LOOKUPS, LIST_LOOKUPS } from './constants';

export function useEmployeeLookups(enabled = true): LookupSources {
  const units = useAuthStore((s) => s.businessUnits);
  const lists = useQueries({
    queries: LIST_LOOKUPS.map((list) => ({ ...getGetLookupValuesQueryOptions(list), enabled })),
  });
  const offices = useGetOfficeOptions({
    query: { queryKey: useUnitKey(getGetOfficeOptionsQueryKey()), enabled },
  });
  const warehouses = useGetWarehouseOptions({
    query: { queryKey: useUnitKey(getGetWarehouseOptionsQueryKey()), enabled },
  });
  const employees = useGetEmployeeOptions({
    query: { queryKey: useUnitKey(getGetEmployeeOptionsQueryKey()), enabled },
  });

  return {
    ...Object.fromEntries(LIST_LOOKUPS.map((list, index) => [list, lists[index]?.data])),
    [EMPLOYEE_LOOKUPS.businessUnit]: units.map((unit) => ({ ...unit, isActive: true })),
    [EMPLOYEE_LOOKUPS.office]: offices.data,
    [EMPLOYEE_LOOKUPS.warehouse]: warehouses.data,
    [EMPLOYEE_LOOKUPS.employee]: employees.data,
  };
}
