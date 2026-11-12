import type { ItemDetail, SaveItemRequest } from '@erp/api-client';
import type { FieldBehavior } from '@erp/metadata-engine';
import { trimmed } from '@/common/master-data';

export type ItemFormValues = SaveItemRequest & {
  departmentId?: number | null;
  categoryId?: number | null;
  subCategoryId?: number | null;
};

export const itemBehaviors = (t: (key: string) => string): Record<string, FieldBehavior> => ({
  barCode: {
    watch: ['isAutoBarcode'],
    state: (v) => ({
      disabled: !!v.isAutoBarcode,
      required: !v.isAutoBarcode,
      placeholder: v.isAutoBarcode ? t('item.f.barcodeAuto') : undefined,
    }),
  },
  masterItemId: {
    watch: ['isChildBarcode'],
    state: (v) => ({ hidden: !v.isChildBarcode, required: true }),
  },
  altUomId: {
    watch: ['uomId'],
    state: (v) => ({ filter: (unit) => unit.id !== v.uomId }),
  },
  altUomConversionQty: {
    watch: ['altUomId'],
    state: (v) => ({ hidden: v.altUomId == null, required: true }),
  },
});

export const copyOf = (item: ItemDetail, name: string): Partial<ItemFormValues> => ({
  ...item,
  code: null,
  name,
  barCode: null,
  isActive: true,
});

export function toRequest(values: ItemFormValues): SaveItemRequest {
  const hasAltUom = values.altUomId != null;
  return {
    code: trimmed(values.code),
    name: values.name.trim(),
    description: trimmed(values.description),
    itemTypeId: values.itemTypeId ?? null,
    hsCode: trimmed(values.hsCode),
    minorCategoryId: values.minorCategoryId,
    isAutoBarcode: !!values.isAutoBarcode,
    barCode: values.isAutoBarcode ? null : trimmed(values.barCode),
    isChildBarcode: !!values.isChildBarcode,
    masterItemId: values.isChildBarcode ? (values.masterItemId ?? null) : null,
    supplierName: trimmed(values.supplierName),
    uomId: values.uomId,
    altUomId: hasAltUom ? values.altUomId : null,
    altUomConversionQty: hasAltUom ? (values.altUomConversionQty ?? null) : null,
    isNoDecimalQty: !!values.isNoDecimalQty,
    averageCostPrice: values.averageCostPrice ?? null,
    lastCostPrice: values.lastCostPrice ?? null,
    salesPrice: values.salesPrice ?? null,
    purchaseSupplyTypeId: values.purchaseSupplyTypeId ?? null,
    purchaseVatPercentage: values.purchaseVatPercentage ?? null,
    salesSupplyTypeId: values.salesSupplyTypeId ?? null,
    salesVatPercentage: values.salesVatPercentage ?? null,
    sdPercentage: values.sdPercentage ?? null,
    isVatInclusive: !!values.isVatInclusive,
    isVatRebatable: !!values.isVatRebatable,
    isSaleable: !!values.isSaleable,
    isPurchase: !!values.isPurchase,
    isNegativeSales: !!values.isNegativeSales,
    isMultipleSalesPrice: !!values.isMultipleSalesPrice,
    imageSource: values.imageSource ?? null,
    customData: values.customData ?? null,
    isActive: !!values.isActive,
  };
}
