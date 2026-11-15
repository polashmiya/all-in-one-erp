import type { SaveEmployeeRequest } from '@erp/api-client';
import type { FieldBehavior } from '@erp/metadata-engine';
import { trimmed } from '@/common/master-data';

export type EmployeeFormValues = Omit<SaveEmployeeRequest, 'user'> & {
  isUser?: boolean;
  user?: { username?: string; password?: string; confirmPassword?: string };
};

const inHomeUnit: FieldBehavior = {
  watch: ['homeBusinessUnitId'],
  state: (v) => ({ filter: (item) => item.parentId === v.homeBusinessUnitId }),
};

export const employeeBehaviors = (employeeId?: number): Record<string, FieldBehavior> => ({
  officeId: inHomeUnit,
  warehouseId: inHomeUnit,
  email: { watch: ['isUser'], state: (v) => ({ required: !!v.isUser }) },
  supervisorId: { watch: [], state: () => ({ filter: (item) => item.id !== employeeId }) },
  lineManagerId: { watch: [], state: () => ({ filter: (item) => item.id !== employeeId }) },
});

export function toRequest(v: EmployeeFormValues): SaveEmployeeRequest {
  return {
    code: trimmed(v.code),
    name: v.name.trim(),
    nameBn: trimmed(v.nameBn),
    imageSource: v.imageSource ?? null,
    designationId: v.designationId ?? null,
    departmentId: v.departmentId ?? null,
    departmentSectionId: v.departmentSectionId ?? null,
    employeeTypeId: v.employeeTypeId ?? null,
    mobile: trimmed(v.mobile),
    email: trimmed(v.email),
    officialContact: trimmed(v.officialContact),
    emergencyMobile: trimmed(v.emergencyMobile),
    fatherName: trimmed(v.fatherName),
    motherName: trimmed(v.motherName),
    dateOfBirth: v.dateOfBirth ?? null,
    genderId: v.genderId ?? null,
    religionId: v.religionId ?? null,
    bloodGroupId: v.bloodGroupId ?? null,
    nid: trimmed(v.nid),
    presentAddress: trimmed(v.presentAddress),
    permanentAddress: trimmed(v.permanentAddress),
    signatureImage: v.signatureImage ?? null,
    homeBusinessUnitId: v.homeBusinessUnitId,
    officeId: v.officeId ?? null,
    warehouseId: v.warehouseId ?? null,
    location: trimmed(v.location),
    joiningDate: v.joiningDate ?? null,
    confirmationDate: v.confirmationDate ?? null,
    terminationDate: v.terminationDate ?? null,
    supervisorId: v.supervisorId ?? null,
    lineManagerId: v.lineManagerId ?? null,
    attendanceId: trimmed(v.attendanceId),
    cardNumber: trimmed(v.cardNumber),
    isSalesRepresentative: !!v.isSalesRepresentative,
    isServiceStaff: !!v.isServiceStaff,
    isActive: !!v.isActive,
    customData: v.customData ?? null,
    user:
      v.isUser && v.user?.username && v.user.password
        ? { username: v.user.username.trim(), password: v.user.password }
        : null,
  };
}
