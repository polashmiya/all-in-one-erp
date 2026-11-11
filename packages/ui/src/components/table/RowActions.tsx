import { MoreOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { ActionButton } from '../actions/ActionButton';
import type { ActionItem } from '../actions/types';
import type { RowAction } from './row-actions';

const toItem = ({ inMenu: _inMenu, ...action }: RowAction): ActionItem => action;

export function RowActions({ actions }: { actions: RowAction[] }) {
  const { t } = useTranslation();
  const visible = actions.filter((action) => !action.hidden);
  const inline = visible.filter((action) => !action.inMenu);
  const menu = visible.filter((action) => action.inMenu);

  return (
    <div className="erp-row-actions">
      {inline.map((action) => (
        <ActionButton
          key={action.key}
          size="small"
          action={{ ...toItem(action), variant: 'text', iconOnly: true }}
        />
      ))}
      {menu.length > 0 && (
        <ActionButton
          size="small"
          action={{
            key: 'more',
            label: t('table.moreActions'),
            icon: <MoreOutlined />,
            variant: 'text',
            iconOnly: true,
            menu: menu.map(toItem),
          }}
        />
      )}
    </div>
  );
}
