import { getGetStockOnHandQueryKey, useGetStockOnHand } from '@erp/api-client';
import { keepPreviousData } from '@tanstack/react-query';
import { useUnitKey } from '@/common/master-data';

export function useOnHand(warehouseId: string | undefined, itemIds: number[]) {
  const params = { warehouseId: warehouseId ?? '', itemIds: [...new Set(itemIds)].sort() };
  const query = useGetStockOnHand(params, {
    query: {
      queryKey: useUnitKey(getGetStockOnHandQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: !!warehouseId && itemIds.length > 0,
    },
  });
  return (itemId: number | null | undefined) => {
    if (!warehouseId || !itemId || !query.data) return undefined;
    const found = query.data.find((row) => row.itemId === itemId);
    return { quantity: found?.quantity ?? 0, cost: found?.cost ?? 0 };
  };
}
