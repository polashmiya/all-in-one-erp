import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';

export const PROCUREMENT_MODULE = 'procurement';
export const PROCUREMENT_API = '/api/erp/procurement';

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
