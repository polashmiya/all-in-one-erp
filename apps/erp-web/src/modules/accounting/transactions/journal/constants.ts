import { featurePath } from '@/common/constants/routes';
import { pathsOf } from '@/common/documents/documents';
import { ACCOUNTING_MODULE } from '../../accounting';

export const JOURNAL_FEATURE = 'accounting.journal';
export const JOURNAL_PATH = featurePath(ACCOUNTING_MODULE, 'journal');

export const journalPaths = pathsOf(JOURNAL_PATH);

export const MANUAL_SOURCE = 'manual';
export const POSTING_SOURCES = [
  'procurement.purchase-receive',
  'procurement.purchase-return',
  'procurement.purchase-payment',
  'inventory.stock-adjustment',
];

export const LINE_REMARKS_MAX = 500;
export const MIN_LINES = 2;
