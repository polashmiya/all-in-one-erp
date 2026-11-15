import type { MenuProps } from 'antd';
import { useNavigate } from 'react-router';
import { useConfirm } from '../overlays/useConfirm';
import type { ActionItem } from './types';

export function useRunAction() {
  const navigate = useNavigate();
  const confirm = useConfirm();

  return async (action: ActionItem) => {
    if (action.confirm) {
      const confirmed = await confirm(action.confirm);
      if (!confirmed) return;
    }
    await action.onClick?.();
    if (action.to) navigate(action.to);
  };
}

export function toMenuItems(
  actions: ActionItem[],
  run: (action: ActionItem) => unknown,
): MenuProps['items'] {
  return actions
    .filter((action) => !action.hidden)
    .map((action) => {
      if (action.divider) return { type: 'divider', key: action.key };
      return {
        key: action.key,
        label: action.description ? (
          <span className="erp-menu-rich">
            <span className="erp-menu-rich__title">{action.label}</span>
            <span className="erp-menu-rich__desc">{action.description}</span>
          </span>
        ) : (
          action.label
        ),
        icon: action.icon,
        disabled: action.disabled,
        danger: action.danger || action.variant === 'danger',
        onClick: ({ domEvent }) => {
          domEvent.stopPropagation();
          void run(action);
        },
      };
    });
}
