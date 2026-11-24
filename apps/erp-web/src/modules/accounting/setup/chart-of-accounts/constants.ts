import { featurePath } from '@/common/constants/routes';
import { ACCOUNTING_MODULE } from '../../accounting';

export const CHART_FEATURE = 'accounting.chart-of-accounts';
export const CHART_PATH = featurePath(ACCOUNTING_MODULE, 'chart-of-accounts');

export const CHART_LOOKUPS = { type: 'account-type', group: 'account-group' } as const;
