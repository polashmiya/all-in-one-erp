import { featurePath } from '@/common/constants/routes';
import { pathsOf } from '@/common/documents/documents';

export const DEPOSIT_FEATURE = 'accounting.tax-deposit';
export const DEPOSIT_PATH = featurePath('accounting', 'tax-deposit');

export const depositPaths = pathsOf(DEPOSIT_PATH);

export const DEPOSIT_LOOKUPS = { type: 'tax-type', method: 'payment-method' } as const;

export const DEPOSIT_LOOKUP_LISTS = [DEPOSIT_LOOKUPS.method];

export const TAX_TYPES = ['vds', 'tds'];
