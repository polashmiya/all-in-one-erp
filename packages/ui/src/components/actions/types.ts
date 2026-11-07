import type { ReactNode } from 'react';
import type { StatusTone } from '../data-display/StatusTag';

export type ActionVariant = 'primary' | 'default' | 'dashed' | 'text' | 'link' | 'soft' | 'danger';

export interface ActionConfirm {
  title: ReactNode;
  description?: ReactNode;
  okText?: string;
  cancelText?: string;
  danger?: boolean;
}

export interface ActionItem {
  key: string;
  label: ReactNode;
  icon?: ReactNode;
  variant?: ActionVariant;
  to?: string;
  onClick?: () => unknown;
  disabled?: boolean;
  hidden?: boolean;
  loading?: boolean;
  tooltip?: ReactNode;
  iconOnly?: boolean;
  danger?: boolean;
  tone?: StatusTone;
  confirm?: ActionConfirm;
  menu?: ActionItem[];
  htmlType?: 'button' | 'submit';
  divider?: boolean;
}

export type ActionSize = 'small' | 'middle' | 'large';
