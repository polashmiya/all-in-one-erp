import { featurePath } from '@/common/constants/routes';

export const PERIOD_CLOSING_FEATURE = 'accounting.period-closing';
export const PERIOD_CLOSING_PATH = featurePath('accounting', 'period-closing');

export const MONTHS = Array.from({ length: 12 }, (_, index) => index + 1);
