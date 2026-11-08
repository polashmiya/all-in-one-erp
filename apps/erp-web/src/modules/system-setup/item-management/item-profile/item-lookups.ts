import {
  getGetItemCategoryOptionsQueryKey,
  getGetItemChoicesQueryKey,
  getGetItemDepartmentOptionsQueryKey,
  getGetItemMinorCategoryOptionsQueryKey,
  getGetItemSubCategoryOptionsQueryKey,
  getGetUomOptionsQueryKey,
  useGetItemCategoryOptions,
  useGetItemChoices,
  useGetItemDepartmentOptions,
  useGetItemMinorCategoryOptions,
  useGetItemSubCategoryOptions,
  useGetUomOptions,
} from '@erp/api-client';
import type { LookupSources } from '@erp/metadata-engine';
import { useUnitKey } from '../common/master-data';

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
  const choices = useGetItemChoices({
    query: { queryKey: getGetItemChoicesQueryKey(), enabled, staleTime: Infinity },
  });

  return {
    'item-department': departments.data,
    'item-category': categories.data,
    'item-sub-category': subCategories.data,
    'item-minor-category': minorCategories.data,
    uom: uoms.data,
    'item-type': choices.data?.itemTypes,
    'vat-supply-type': choices.data?.supplyTypes,
  } satisfies LookupSources;
}

export type ItemLookups = ReturnType<typeof useItemLookups>;
