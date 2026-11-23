import type { StatusTone } from '@erp/ui';

export const REVIEW_COMMENT_MAX = 500;

export const STEP_TONES: Record<string, StatusTone> = {
  pending: 'neutral',
  approved: 'success',
  rejected: 'danger',
};
