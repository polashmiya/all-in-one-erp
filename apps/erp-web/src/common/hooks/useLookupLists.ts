import { getGetLookupValuesQueryOptions } from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { useQueries } from '@tanstack/react-query';

export function useLookupLists(lists: readonly string[], enabled = true): LookupSources {
  const results = useQueries({
    queries: lists.map((list) => ({ ...getGetLookupValuesQueryOptions(list), enabled })),
  });
  return Object.fromEntries(lists.map((list, index) => [list, results[index]?.data]));
}
