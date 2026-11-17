import type { SaveSupplierRequest } from '@erp/api-client';
import { trimmed } from '@/common/master-data';

export type PartnerRequest = Omit<SaveSupplierRequest, 'tolerance'>;

export function partnerRequest(v: PartnerRequest): PartnerRequest {
  return {
    code: trimmed(v.code),
    name: v.name.trim(),
    billingName: trimmed(v.billingName),
    groupId: v.groupId ?? null,
    mobile: trimmed(v.mobile),
    email: trimmed(v.email),
    contactPerson: trimmed(v.contactPerson),
    contactMobile: trimmed(v.contactMobile),
    address: trimmed(v.address),
    city: trimmed(v.city),
    districtId: v.districtId ?? null,
    thanaId: v.thanaId ?? null,
    isForeign: !!v.isForeign,
    creditLimit: v.creditLimit ?? null,
    paymentDays: v.paymentDays ?? null,
    binNumber: trimmed(v.binNumber),
    tinNumber: trimmed(v.tinNumber),
    tradeLicenseNumber: trimmed(v.tradeLicenseNumber),
    nid: trimmed(v.nid),
    centralVatRegistrationNo: trimmed(v.centralVatRegistrationNo),
    isVatNotApplicable: !!v.isVatNotApplicable,
    isVdsApplicable: !!v.isVdsApplicable,
    remarks: trimmed(v.remarks),
    isActive: !!v.isActive,
    customData: v.customData ?? null,
  };
}
