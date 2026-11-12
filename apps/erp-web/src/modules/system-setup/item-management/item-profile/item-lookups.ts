import {
  getGetItemCategoryOptionsQueryKey,
  getGetItemDepartmentOptionsQueryKey,
  getGetItemMinorCategoryOptionsQueryKey,
  getGetItemSubCategoryOptionsQueryKey,
  getGetLookupValuesQueryKey,
  getGetUomOptionsQueryKey,
  useGetItemCategoryOptions,
  useGetItemDepartmentOptions,
  useGetItemMinorCategoryOptions,
  useGetItemSubCategoryOptions,
  useGetLookupValues,
  useGetUomOptions,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { useUnitKey } from '@/common/master-data';
import { ITEM_LOOKUPS } from '../item-management';

export function useItemLookups(enabled = true) {
  const departments = useGetItemDepartmentOptions({
    query: { queryKey: useUnitKey(getGetItemDepartmentOptionsQueryKey()), enabled },
  });
  const categories = useGetItemCategoryOptions(undefined, {
    query: { queryKey: useUnitKey(getGetItemCategoryOptionsQueryKey()), enabled },
  });
  const subCategories = useGetItemSubCategoryOptions(undefined, {
    query: { queryKey: useUnitKey(getGetItemSubCategoryOptionsQueryKey()), enabled },
  });
  const minorCategories = useGetItemMinorCategoryOptions(undefined, {
    query: { queryKey: useUnitKey(getGetItemMinorCategoryOptionsQueryKey()), enabled },
  });
  const uoms = useGetUomOptions({
    query: { queryKey: useUnitKey(getGetUomOptionsQueryKey()), enabled },
  });
  const itemTypes = useGetLookupValues(ITEM_LOOKUPS.itemType, {
    query: { queryKey: getGetLookupValuesQueryKey(ITEM_LOOKUPS.itemType), enabled },
  });
  const supplyTypes = useGetLookupValues(ITEM_LOOKUPS.supplyType, {
    query: { queryKey: getGetLookupValuesQueryKey(ITEM_LOOKUPS.supplyType), enabled },
  });

  return {
    [ITEM_LOOKUPS.department]: departments.data,
    [ITEM_LOOKUPS.category]: categories.data,
    [ITEM_LOOKUPS.subCategory]: subCategories.data,
    [ITEM_LOOKUPS.minorCategory]: minorCategories.data,
    [ITEM_LOOKUPS.uom]: uoms.data,
    [ITEM_LOOKUPS.itemType]: itemTypes.data,
    [ITEM_LOOKUPS.supplyType]: supplyTypes.data,
  } satisfies LookupSources;
}

export type ItemLookups = ReturnType<typeof useItemLookups>;
