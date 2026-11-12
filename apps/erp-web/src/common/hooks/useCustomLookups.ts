import { getGetLookupValuesQueryOptions, type FeatureConfig } from '@erp/api-client';
import { fieldsOf, type LookupSources } from '@erp/metadata-engine';
import { useQueries } from '@tanstack/react-query';

export function useCustomLookups(config: FeatureConfig): LookupSources {
  const lists = [
    ...new Set(
      fieldsOf(config)
        .filter((field) => field.custom)
        .flatMap((field) => field.lookup ?? []),
    ),
  ];
  const results = useQueries({
    queries: lists.map((list) => getGetLookupValuesQueryOptions(list)),
  });

  return Object.fromEntries(lists.map((list, index) => [list, results[index]?.data]));
}
