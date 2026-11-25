import {
  getGetChargeAccountOptionsQueryKey,
  getGetCustomerOptionsQueryKey,
  getGetEmployeeOptionsQueryKey,
  useGetChargeAccountOptions,
  useGetCustomerOptions,
  useGetEmployeeOptions,
  type CustomerOption,
  type SalesOrderLineDetail,
  type SaveSalesOrderLine,
  type SaveSalesOrderRequest,
} from '@erp/api-client';
import type { FieldBehavior, LookupSources } from '@erp/metadata-engine';
import { Form } from 'antd';
import { DOCUMENT_LOOKUPS, useUnitOffices, useUnitWarehouses } from '@/common/documents/documents';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed, useUnitKey } from '@/common/master-data';
import { discountFields, discountOf, type SalesHeader, type SalesLine } from './pricing';

export const SALES_LOOKUPS = {
  ...DOCUMENT_LOOKUPS,
  chargeAccount: 'charge-account',
  paymentMethod: 'payment-method',
} as const;

export const SALES_LOOKUP_LISTS = [
  'price-type',
  'payment-type',
  SALES_LOOKUPS.paymentMethod,
  'shipping-address',
  'free-type',
  'currency',
] as const;

export const FORM_COLUMNS = 6;
export const DESCRIPTION_MAX = 2000;
export const SHELF_LIFE_MAX = 150;
export const WARRANTY_MAX = 1200;
export const SD_MAX = 1000;

type SharedLine = Pick<
  SaveSalesOrderLine,
  'itemDescription' | 'warrantyInMonth' | 'shelfLife' | 'itemId'
>;

export interface SalesLineValue extends SalesLine, Partial<SharedLine> {
  itemCode?: string;
  itemName?: string;
  uomName?: string;
}

type OwnOrderFields =
  | 'salesQuotationId'
  | 'advanceAmount'
  | 'dueLimit'
  | 'customerPoAmount'
  | 'customerPoRefAmount'
  | 'salesTermId'
  | 'dmsInvoice'
  | 'isForeign'
  | 'lcTypeId'
  | 'lcNo'
  | 'paymentTermsId'
  | 'isAllowPartialShipment'
  | 'isAllowTransitShipment'
  | 'review'
  | 'lines';

export type SalesDocumentValues = Omit<SaveSalesOrderRequest, OwnOrderFields>;

export function useSalesLookups(lists: readonly string[], enabled = true): LookupSources {
  const values = useLookupLists(lists, enabled);
  const offices = useUnitOffices(enabled);
  const warehouses = useUnitWarehouses(enabled);
  const customers = useGetCustomerOptions({
    query: { queryKey: useUnitKey(getGetCustomerOptionsQueryKey()), enabled },
  });
  const employees = useGetEmployeeOptions({
    query: { queryKey: useUnitKey(getGetEmployeeOptionsQueryKey()), enabled },
  });
  const accounts = useGetChargeAccountOptions({
    query: { queryKey: useUnitKey(getGetChargeAccountOptionsQueryKey()), enabled },
  });

  return {
    ...values,
    [SALES_LOOKUPS.office]: offices,
    [SALES_LOOKUPS.warehouse]: warehouses,
    [SALES_LOOKUPS.customer]: customers.data,
    [SALES_LOOKUPS.employee]: employees.data,
    [SALES_LOOKUPS.chargeAccount]: accounts.data,
  };
}

export const emptyLine = (): SalesLineValue => ({
  quantity: 1,
  price: null,
  discount: null,
  discountType: 'percent',
});

export function fromCustomer(customer: CustomerOption) {
  return {
    shippingContactPerson: customer.contactPerson,
    shippingAddressName: customer.address,
    ...(customer.salesRepresentativeId && { salesForceId: customer.salesRepresentativeId }),
  };
}

export function documentRequest(v: SalesDocumentValues): SalesDocumentValues {
  return {
    code: trimmed(v.code),
    orderDate: v.orderDate,
    deliveryDate: v.deliveryDate,
    customerId: v.customerId,
    officeId: v.officeId ?? null,
    warehouseId: v.warehouseId,
    salesForceId: v.salesForceId ?? null,
    priceTypeId: v.priceTypeId ?? null,
    projectName: trimmed(v.projectName),
    referredByPartnerId: v.referredByPartnerId ?? null,
    customerPo: trimmed(v.customerPo),
    customerPoDate: v.customerPoDate ?? null,
    paymentTypeId: v.paymentTypeId ?? null,
    paymentTermDays: v.paymentTermDays ?? null,
    paymentMethodId: v.paymentMethodId ?? null,
    bankAccountId: v.bankAccountId ?? null,
    instrumentNo: trimmed(v.instrumentNo),
    instrumentDate: v.instrumentDate ?? null,
    shippingContactPerson: trimmed(v.shippingContactPerson),
    shippingAddressId: v.shippingAddressId ?? null,
    shippingAddressName: trimmed(v.shippingAddressName),
    isVatInclusive: !!v.isVatInclusive,
    isGrossDiscount: !!v.isGrossDiscount,
    headerDiscountPercentage: v.headerDiscountPercentage ?? null,
    headerDiscount: v.headerDiscount ?? null,
    specialDiscountAmount: v.specialDiscountAmount ?? null,
    othersCost: v.othersCost ?? null,
    otherChargeAccountId: v.otherChargeAccountId ?? null,
    extraCharge: v.extraCharge ?? null,
    extraChargeAccountId: v.extraChargeAccountId ?? null,
    deductionLabel: trimmed(v.deductionLabel),
    deductionValue: v.deductionValue ?? null,
    freeTypeId: v.freeTypeId ?? null,
    currencyId: v.currencyId ?? null,
    exchangeRate: v.exchangeRate ?? null,
    isAdvanceAdjust: !!v.isAdvanceAdjust,
    remarks: trimmed(v.remarks),
    customData: v.customData ?? null,
  };
}

export function lineRequest(line: SalesLineValue) {
  return {
    itemId: line.itemId ?? 0,
    itemDescription: trimmed(line.itemDescription),
    quantity: line.quantity ?? 0,
    price: line.price ?? null,
    ...discountFields(line),
    sdPercentage: line.sdPercentage ?? null,
    vatPercentage: line.vatPercentage ?? null,
    aitPercentage: line.aitPercentage ?? null,
    warrantyInMonth: line.warrantyInMonth ?? null,
    isOfferItem: !!line.isOfferItem,
    shelfLife: trimmed(line.shelfLife),
  };
}

type SavedLine = Omit<
  SalesOrderLineDetail,
  'deliveredQuantity' | 'scheduledQuantity' | 'demandQuantity' | 'fobRate' | 'freightRate'
>;

export function lineValue(line: SavedLine): SalesLineValue {
  return {
    itemId: line.itemId,
    itemCode: line.itemCode,
    itemName: line.itemName,
    itemDescription: line.itemDescription,
    uomName: line.uomName,
    quantity: line.quantity,
    price: line.price,
    ...discountOf(line),
    sdPercentage: line.sdPercentage,
    vatPercentage: line.vatPercentage,
    aitPercentage: line.aitPercentage,
    warrantyInMonth: line.warrantyInMonth,
    isOfferItem: line.isOfferItem,
    shelfLife: line.shelfLife,
  };
}

export const discountBehaviors: Record<string, FieldBehavior> = {
  headerDiscountPercentage: {
    watch: ['isGrossDiscount'],
    state: (values) => ({ disabled: !values.isGrossDiscount }),
  },
  headerDiscount: {
    watch: ['isGrossDiscount', 'headerDiscountPercentage'],
    state: (values) => ({
      disabled: !values.isGrossDiscount || Number(values.headerDiscountPercentage ?? 0) > 0,
    }),
  },
};

export const keepVisible = (behavior: FieldBehavior): FieldBehavior => ({
  ...behavior,
  state: (values) => {
    const { hidden, ...state } = behavior.state(values);
    return hidden ? { disabled: true } : state;
  },
});

export function useSalesHeader(): SalesHeader {
  const form = Form.useFormInstance();
  const value = (name: number | null | undefined) => name ?? 0;
  const othersCost: number | null = Form.useWatch('othersCost', form);
  const extraCharge: number | null = Form.useWatch('extraCharge', form);
  const deduction: number | null = Form.useWatch('deductionValue', form);
  const special: number | null = Form.useWatch('specialDiscountAmount', form);
  return {
    isVatInclusive: Form.useWatch('isVatInclusive', form),
    isGrossDiscount: Form.useWatch('isGrossDiscount', form),
    discountPercentage: Form.useWatch('headerDiscountPercentage', form),
    discountAmount: Form.useWatch('headerDiscount', form),
    charges: value(othersCost) + value(extraCharge),
    reductions: value(deduction) + value(special),
  };
}
