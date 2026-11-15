import type { RouteObject } from 'react-router';
import { itemCategoryRoutes } from './item-management/item-category/routes';
import { itemDepartmentRoutes } from './item-management/item-department/routes';
import { itemMinorCategoryRoutes } from './item-management/item-minor-category/routes';
import { itemProfileRoutes } from './item-management/item-profile/routes';
import { itemSubCategoryRoutes } from './item-management/item-sub-category/routes';
import { uomRoutes } from './item-management/uom/routes';
import { employeeRoutes } from './user-management/employee/routes';
import { userRoleRoutes } from './user-management/user-role/routes';
import { userRoutes } from './user-management/user/routes';

export const systemSetupRoutes: RouteObject[] = [
  ...itemProfileRoutes,
  ...itemDepartmentRoutes,
  ...itemCategoryRoutes,
  ...itemSubCategoryRoutes,
  ...itemMinorCategoryRoutes,
  ...uomRoutes,
  ...userRoutes,
  ...employeeRoutes,
  ...userRoleRoutes,
];
