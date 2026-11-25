import {
  approveSalesQuotation,
  cancelSalesQuotation,
  closeSalesQuotation,
  deleteSalesQuotation,
  getGetCustomerOptionsQueryKey,
  getGetEmployeeOptionsQueryKey,
  getGetSalesQuotationApprovalQueryKey,
  getSalesQuotationApproval,
  rejectSalesQuotation,
  useGetCustomerOptions,
  useGetEmployeeOptions,
  type CustomerOption,
  type SalesQuotationDetail,
  type SaveSalesQuotationRequest,
} from '@erp/api-client';
import type { FieldBehavior, LookupSources } from '@erp/metadata-engine';
import { API_DATE_FORMAT, formatDate } from '@erp/ui';
import dayjs from 'dayjs';
import { useUnitOffices } from '@/common/documents/documents';
import type { ApprovalCalls } from '@/common/documents/useApprovalActions';
import { useLookupLists } from '@/common/hooks/useLookupLists';
import { trimmed, useUnitKey } from '@/common/master-data';
import { discountFields, discountOf } from '../../pricing';
import { emptyLine, type SalesLineValue } from '../../sales-document';
import {
  FIRST_VERSION,
  QUOTATION_LOOKUP_LISTS,
  QUOTATION_LOOKUPS,
  VALIDITY_DAYS,
} from './constants';

export interface QuotationLine extends SalesLineValue {
  deliveryDate?: string | null;
}

export type QuotationValues = Omit<SaveSalesQuotationRequest, 'lines'> & {
  lines: QuotationLine[];
};

export function useQuotationLookups(enabled = true): LookupSources {
  const lists = useLookupLists(QUOTATION_LOOKUP_LISTS, enabled);
  const offices = useUnitOffices(enabled);
  const customers = useGetCustomerOptions({
    query: { queryKey: useUnitKey(getGetCustomerOptionsQueryKey()), enabled },
  });
  const employees = useGetEmployeeOptions({
    query: { queryKey: useUnitKey(getGetEmployeeOptionsQueryKey()), enabled },
  });

  return {
    ...lists,
    [QUOTATION_LOOKUPS.office]: offices,
    [QUOTATION_LOOKUPS.customer]: customers.data,
    [QUOTATION_LOOKUPS.employee]: employees.data,
  };
}

export const quotationBehaviors: Record<string, FieldBehavior> = {
  discountPercentage: {
    watch: ['isGrossDiscount'],
    state: (values) => ({ disabled: !values.isGrossDiscount }),
  },
  discountAmount: {
    watch: ['isGrossDiscount', 'discountPercentage'],
    state: (values) => ({
      disabled: !values.isGrossDiscount || Number(values.discountPercentage ?? 0) > 0,
    }),
  },
};

export function newQuotation(): Partial<QuotationValues> {
  const today = formatDate(new Date(), API_DATE_FORMAT);
  return {
    quotationDate: today,
    validityDate: dayjs().add(VALIDITY_DAYS, 'day').format(API_DATE_FORMAT),
    version: FIRST_VERSION,
    versionDate: today,
    lines: [emptyLine()],
  };
}

export function fromCustomer(customer: CustomerOption): Partial<QuotationValues> {
  return {
    billingContactPerson: customer.contactPerson,
    billingPhoneNo: customer.phone,
    billingAddress: customer.address,
    shippingContactPerson: customer.contactPerson,
    shippingAddressName: customer.address,
    ...(customer.salesRepresentativeId && { salesForceId: customer.salesRepresentativeId }),
  };
}

export function fromDetail(detail: SalesQuotationDetail): QuotationValues {
  return {
    ...detail,
    lines: detail.lines.map((line) => ({
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
      deliveryDate: line.deliveryDate,
    })),
  };
}

export function toRequest(v: QuotationValues): SaveSalesQuotationRequest {
  return {
    code: trimmed(v.code),
    quotationTypeId: v.quotationTypeId ?? null,
    version: trimmed(v.version),
    versionDate: v.versionDate ?? null,
    quotationDate: v.quotationDate,
    validityDate: v.validityDate,
    customerId: v.customerId,
    officeId: v.officeId ?? null,
    salesForceId: v.salesForceId ?? null,
    reference: trimmed(v.reference),
    billingContactPerson: trimmed(v.billingContactPerson),
    billingPhoneNo: trimmed(v.billingPhoneNo),
    billingAddress: trimmed(v.billingAddress),
    shippingContactPerson: trimmed(v.shippingContactPerson),
    shippingAddressId: v.shippingAddressId ?? null,
    shippingAddressName: trimmed(v.shippingAddressName),
    isVatInclusive: !!v.isVatInclusive,
    isGrossDiscount: !!v.isGrossDiscount,
    discountPercentage: v.discountPercentage ?? null,
    discountAmount: v.discountAmount ?? null,
    deliveryCharge: v.deliveryCharge ?? null,
    extraCharge: v.extraCharge ?? null,
    subjects: trimmed(v.subjects),
    bodyNarration: trimmed(v.bodyNarration),
    footerNarration: trimmed(v.footerNarration),
    narration: trimmed(v.narration),
    lines: v.lines.map((line) => ({
      itemId: line.itemId ?? 0,
      itemDescription: trimmed(line.itemDescription),
      quantity: line.quantity ?? 0,
      price: line.price ?? null,
      ...discountFields(line),
      sdPercentage: line.sdPercentage ?? null,
      vatPercentage: line.vatPercentage ?? null,
      aitPercentage: line.aitPercentage ?? null,
      warrantyInMonth: line.warrantyInMonth ?? null,
      deliveryDate: trimmed(line.deliveryDate),
    })),
    customData: v.customData ?? null,
  };
}

export const quotationCalls: ApprovalCalls = {
  approve: (id, data) => approveSalesQuotation(id, data),
  reject: (id, data) => rejectSalesQuotation(id, data),
  cancel: (id, data) => cancelSalesQuotation(id, data),
  close: (id) => closeSalesQuotation(id),
  remove: (id) => deleteSalesQuotation(id),
  progress: (id) => getSalesQuotationApproval(id),
  progressKey: (id) => getGetSalesQuotationApprovalQueryKey(id),
};
