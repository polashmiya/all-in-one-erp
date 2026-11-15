export const EMPLOYEE_ASIDE_SECTIONS = ['photo', 'status'];

export const EMPLOYEE_LOOKUPS = {
  designation: 'designation',
  department: 'employee-department',
  departmentSection: 'department-section',
  employeeType: 'employee-type',
  gender: 'gender',
  religion: 'religion',
  bloodGroup: 'blood-group',
  businessUnit: 'business-unit',
  office: 'office',
  warehouse: 'warehouse',
  employee: 'employee',
} as const;

export const LIST_LOOKUPS = [
  EMPLOYEE_LOOKUPS.designation,
  EMPLOYEE_LOOKUPS.department,
  EMPLOYEE_LOOKUPS.departmentSection,
  EMPLOYEE_LOOKUPS.employeeType,
  EMPLOYEE_LOOKUPS.gender,
  EMPLOYEE_LOOKUPS.religion,
  EMPLOYEE_LOOKUPS.bloodGroup,
];

export const USER_FILTER_VALUES = ['yes', 'no'] as const;
