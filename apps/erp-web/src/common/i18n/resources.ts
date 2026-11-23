import { mergeResources, type AppResources } from '@erp/ui';
import { authResources } from '@/modules/auth/i18n';
import { dashboardResources } from '@/modules/dashboard/i18n';
import { masterDataResources } from '@/common/master-data';
import { itemCategoryResources } from '@/modules/system-setup/item-management/item-category/i18n';
import { itemDepartmentResources } from '@/modules/system-setup/item-management/item-department/i18n';
import { itemMinorCategoryResources } from '@/modules/system-setup/item-management/item-minor-category/i18n';
import { itemProfileResources } from '@/modules/system-setup/item-management/item-profile/i18n';
import { itemSubCategoryResources } from '@/modules/system-setup/item-management/item-sub-category/i18n';
import { uomResources } from '@/modules/system-setup/item-management/uom/i18n';
import { employeeResources } from '@/modules/system-setup/user-management/employee/i18n';
import { userRoleResources } from '@/modules/system-setup/user-management/user-role/i18n';
import { userResources } from '@/modules/system-setup/user-management/user/i18n';
import { partnerResources } from '@/modules/system-setup/partner-management/i18n';
import { customerProfileResources } from '@/modules/system-setup/partner-management/customer-profile/i18n';
import { supplierProfileResources } from '@/modules/system-setup/partner-management/supplier-profile/i18n';
import { approvalResources } from '@/common/approval/i18n';
import { myApprovalsResources } from '@/modules/approval/my-approvals/i18n';
import { procurementResources } from '@/modules/procurement/i18n';
import { purchaseRequisitionResources } from '@/modules/procurement/purchase/purchase-requisition/i18n';
import { purchaseOrderResources } from '@/modules/procurement/purchase/purchase-order/i18n';
import { purchaseReceiveResources } from '@/modules/procurement/purchase/purchase-receive/i18n';

const commonErpResources: AppResources = {
  en: {
    'shell.productName': 'myErp · ERP',
    'shell.accountAdmin': 'Account admin',
    'shell.businessUnitCount_one': '{{count}} business unit',
    'shell.businessUnitCount_other': '{{count}} business units',
    'bu.switched': 'Switched to {{name}}',
    'placeholder.featureTitle': 'Configuration-driven screen',
    'placeholder.featureText':
      'This screen is rendered by the metadata engine from the configuration published for your business unit. It becomes available once the feature is built and enabled.',
    'placeholder.sectionTitle': 'Coming soon',
    'placeholder.sectionText':
      'This area is part of the roadmap and will be available in a later release.',
    'placeholder.featureKey': 'Feature key',
    'placeholder.status': 'Not configured',
  },
  bn: {
    'shell.productName': 'myErp · ERP',
    'shell.accountAdmin': 'অ্যাকাউন্ট অ্যাডমিন',
    'shell.businessUnitCount_one': '{{count}}টি বিজনেস ইউনিট',
    'shell.businessUnitCount_other': '{{count}}টি বিজনেস ইউনিট',
    'bu.switched': '{{name}}-এ পরিবর্তন করা হয়েছে',
    'placeholder.featureTitle': 'কনফিগারেশন-চালিত স্ক্রিন',
    'placeholder.featureText':
      'এই স্ক্রিনটি আপনার বিজনেস ইউনিটের প্রকাশিত কনফিগারেশন থেকে মেটাডেটা ইঞ্জিন দিয়ে তৈরি হয়। ফিচারটি তৈরি ও চালু হলে এটি ব্যবহার করা যাবে।',
    'placeholder.sectionTitle': 'শীঘ্রই আসছে',
    'placeholder.sectionText': 'এই অংশটি রোডম্যাপে আছে এবং পরবর্তী রিলিজে পাওয়া যাবে।',
    'placeholder.featureKey': 'ফিচার কী',
    'placeholder.status': 'কনফিগার করা হয়নি',
  },
};

export const erpResources = mergeResources(
  commonErpResources,
  authResources,
  dashboardResources,
  itemProfileResources,
  masterDataResources,
  itemDepartmentResources,
  itemCategoryResources,
  itemSubCategoryResources,
  itemMinorCategoryResources,
  uomResources,
  userResources,
  employeeResources,
  userRoleResources,
  partnerResources,
  customerProfileResources,
  supplierProfileResources,
  approvalResources,
  myApprovalsResources,
  procurementResources,
  purchaseRequisitionResources,
  purchaseOrderResources,
  purchaseReceiveResources,
);
