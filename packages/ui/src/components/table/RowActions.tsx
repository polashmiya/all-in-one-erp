import { Switch, Tooltip } from 'antd';
import type { ReactNode } from 'react';
import { ActionButton } from '../actions/ActionButton';

export interface RowAction {
  key: string;
  label: string;
  icon?: ReactNode;
  checked?: boolean;
  hidden?: boolean;
  onClick: () => unknown;
}

export function RowActions({ actions }: { actions: RowAction[] }) {
  return (
    <div className="erp-row-actions">
      {actions
        .filter((action) => !action.hidden)
        .map((action) =>
          action.checked === undefined ? (
            <ActionButton
              key={action.key}
              size="small"
              action={{ ...action, variant: 'text', iconOnly: true, tone: 'primary' }}
            />
          ) : (
            <Tooltip key={action.key} title={action.label}>
              <Switch
                size="small"
                checked={action.checked}
                aria-label={action.label}
                onClick={(_, event) => event.stopPropagation()}
                onChange={() => void action.onClick()}
              />
            </Tooltip>
          ),
        )}
    </div>
  );
}
