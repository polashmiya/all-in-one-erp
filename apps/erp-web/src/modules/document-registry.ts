import { adjustmentPaths } from '@/modules/inventory/stock/stock-adjustment/constants';
import { transferPaths } from '@/modules/inventory/stock/stock-transfer/constants';
import { journalPaths } from '@/modules/accounting/transactions/journal/constants';
import { depositPaths } from '@/modules/accounting/transactions/tax-deposit/constants';
import {
  PERIOD_CLOSING_FEATURE,
  PERIOD_CLOSING_PATH,
} from '@/modules/accounting/setup/period-closing/constants';
import { orderPaths } from '@/modules/procurement/purchase/purchase-order/constants';
import { paymentPaths } from '@/modules/procurement/purchase/purchase-payment/constants';
import { receivePaths } from '@/modules/procurement/purchase/purchase-receive/constants';
import { requisitionPaths } from '@/modules/procurement/purchase/purchase-requisition/constants';
import { returnPaths } from '@/modules/procurement/purchase/purchase-return/constants';
import { collectionPaths } from '@/modules/sales/sales/sales-collection/constants';
import { deliveryPaths } from '@/modules/sales/sales/sales-delivery/constants';
import { orderPaths as salesOrderPaths } from '@/modules/sales/sales/sales-order/constants';
import { quotationPaths } from '@/modules/sales/sales/sales-quotation/constants';
import { returnPaths as salesReturnPaths } from '@/modules/sales/sales/sales-return/constants';

export interface DocumentLink {
  label: string;
  path: (id: number) => string;
}

export const DOCUMENTS = {
  'sales.sales-quotation': { label: 'document.type.quotation', path: quotationPaths.view },
  'sales.sales-order': { label: 'document.type.salesOrder', path: salesOrderPaths.view },
  'sales.sales-delivery': { label: 'document.type.delivery', path: deliveryPaths.view },
  'sales.sales-return': { label: 'document.type.salesReturn', path: salesReturnPaths.view },
  'sales.sales-collection': { label: 'document.type.collection', path: collectionPaths.view },
  'procurement.purchase-requisition': {
    label: 'document.type.requisition',
    path: requisitionPaths.view,
  },
  'procurement.purchase-order': { label: 'document.type.order', path: orderPaths.view },
  'procurement.purchase-receive': { label: 'document.type.receive', path: receivePaths.view },
  'procurement.purchase-return': { label: 'document.type.return', path: returnPaths.view },
  'procurement.purchase-payment': { label: 'document.type.payment', path: paymentPaths.view },
  'inventory.stock-adjustment': { label: 'document.type.adjustment', path: adjustmentPaths.view },
  'inventory.stock-transfer': { label: 'document.type.transfer', path: transferPaths.view },
  'accounting.journal': { label: 'document.type.journal', path: journalPaths.view },
  'accounting.tax-deposit': { label: 'document.type.taxDeposit', path: depositPaths.view },
} satisfies Record<string, DocumentLink>;

export type DocumentFeature = keyof typeof DOCUMENTS;

const LINKS: Record<string, DocumentLink> = {
  ...DOCUMENTS,
  [PERIOD_CLOSING_FEATURE]: {
    label: 'document.type.closing',
    path: () => PERIOD_CLOSING_PATH,
  },
};

export const documentLink = (feature: string | null | undefined): DocumentLink | undefined =>
  feature ? LINKS[feature] : undefined;
