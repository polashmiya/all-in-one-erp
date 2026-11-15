import { DownOutlined } from '@ant-design/icons';
import { Button, Dropdown, Space, Tooltip, type ButtonProps } from 'antd';
import { useTranslation } from 'react-i18next';
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
  const { t } = useTranslation();
  const run = useRunAction();
  if (action.hidden) return null;

  const split = !!action.split && !!action.menu?.length;
  const hasMenu = !split && !!action.menu?.length;
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

  if (split) {
    control = (
      <Space.Compact className="erp-split-btn">
        {control}
        <Dropdown
          trigger={['click']}
          placement="bottomRight"
          rootClassName="erp-split-menu"
          menu={{ items: toMenuItems(action.menu!, run) }}
        >
          <Button
            {...BUTTON_STYLE[action.variant ?? 'default']}
            size={size}
            icon={<DownOutlined />}
            disabled={action.disabled || action.loading}
            aria-label={t('common.moreOptions')}
          />
        </Dropdown>
      </Space.Compact>
    );
  }

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
