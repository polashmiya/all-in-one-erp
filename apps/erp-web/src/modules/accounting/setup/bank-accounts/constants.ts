import { featurePath } from '@/common/constants/routes';
import { ACCOUNTING_MODULE } from '../../accounting';

export const BANK_FEATURE = 'accounting.bank-accounts';
export const BANK_PATH = featurePath(ACCOUNTING_MODULE, 'bank-accounts');

export const BANK_LOOKUPS = { type: 'bank-account-type', account: 'chart-of-account' } as const;

export const BANK_TYPES = ['bank', 'mobile'];
