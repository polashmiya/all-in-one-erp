import type { RouteObject } from 'react-router';
import { stockBalanceRoutes } from './reports/stock-balance/routes';
import { stockLedgerRoutes } from './reports/stock-ledger/routes';
import { stockAdjustmentRoutes } from './stock/stock-adjustment/routes';
import { stockTransferRoutes } from './stock/stock-transfer/routes';

export const inventoryRoutes: RouteObject[] = [
  ...stockAdjustmentRoutes,
  ...stockTransferRoutes,
  ...stockBalanceRoutes,
  ...stockLedgerRoutes,
];
