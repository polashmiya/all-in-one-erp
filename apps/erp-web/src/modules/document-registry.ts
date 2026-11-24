import { adjustmentPaths } from '@/modules/inventory/stock/stock-adjustment/constants';
import { transferPaths } from '@/modules/inventory/stock/stock-transfer/constants';
import { journalPaths } from '@/modules/accounting/transactions/journal/constants';
import { orderPaths } from '@/modules/procurement/purchase/purchase-order/constants';
import { paymentPaths } from '@/modules/procurement/purchase/purchase-payment/constants';
import { receivePaths } from '@/modules/procurement/purchase/purchase-receive/constants';
import { requisitionPaths } from '@/modules/procurement/purchase/purchase-requisition/constants';
import { returnPaths } from '@/modules/procurement/purchase/purchase-return/constants';

export interface DocumentLink {
  label: string;
  path: (id: number) => string;
}

export const DOCUMENTS = {
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
} satisfies Record<string, DocumentLink>;

export type DocumentFeature = keyof typeof DOCUMENTS;

export const documentLink = (feature: string | null | undefined): DocumentLink | undefined =>
  feature ? (DOCUMENTS as Record<string, DocumentLink>)[feature] : undefined;
