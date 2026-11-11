import type { ReactNode } from 'react';
import type { ActionConfirm } from '../actions/types';
import { ROW_ACTION_FALLBACK, ROW_ACTION_PADDING, ROW_ACTION_SIZE } from './constants';

export interface RowAction {
  key: string;
  label: string;
  icon?: ReactNode;
  hidden?: boolean;
  inMenu?: boolean;
  danger?: boolean;
  confirm?: ActionConfirm;
  onClick: () => unknown;
}

export function rowActionsWidth(actions?: RowAction[]) {
  const visible = actions?.filter((action) => !action.hidden);
  const buttons = visible
    ? visible.filter((action) => !action.inMenu).length +
      (visible.some((action) => action.inMenu) ? 1 : 0)
    : ROW_ACTION_FALLBACK;
  return buttons * ROW_ACTION_SIZE + ROW_ACTION_PADDING;
}
