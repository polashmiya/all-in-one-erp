import { DownOutlined } from '@ant-design/icons';
import { Button, Dropdown, Tooltip, type ButtonProps } from 'antd';
import type { ActionItem, ActionSize, ActionVariant } from './types';
import { toMenuItems, useRunAction } from './useRunAction';

const BUTTON_STYLE: Record<ActionVariant, ButtonProps> = {
  primary: { type: 'primary' },
  default: { type: 'default' },
  dashed: { type: 'dashed' },
  text: { type: 'text' },
  link: { type: 'link' },
  soft: { color: 'primary', variant: 'filled' },
  danger: { danger: true },
};

export function ActionButton({ action, size }: { action: ActionItem; size?: ActionSize }) {
  const run = useRunAction();
  if (action.hidden) return null;

  const hasMenu = !!action.menu?.length;
  const iconOnly = !!action.iconOnly && !!action.icon;

  let control = (
    <Button
      {...BUTTON_STYLE[action.variant ?? 'default']}
      danger={action.danger || action.variant === 'danger'}
      className={action.tone && `erp-tone-btn erp-tag--${action.tone}`}
      size={size}
      icon={action.icon}
      loading={action.loading}
      disabled={action.disabled}
      htmlType={action.htmlType ?? 'button'}
      aria-label={iconOnly && typeof action.label === 'string' ? action.label : undefined}
      onClick={(event) => {
        event.stopPropagation();
        if (!hasMenu) void run(action);
      }}
    >
      {!iconOnly && action.label}
      {!iconOnly && hasMenu && <DownOutlined className="erp-action__caret" />}
    </Button>
  );

  if (hasMenu) {
    control = (
      <Dropdown trigger={['click']} menu={{ items: toMenuItems(action.menu!, run) }}>
        {control}
      </Dropdown>
    );
  }

  if (action.tooltip || iconOnly) {
    control = <Tooltip title={action.tooltip ?? action.label}>{control}</Tooltip>;
  }

  return control;
}
