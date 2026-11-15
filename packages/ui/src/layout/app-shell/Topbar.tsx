import { BellOutlined, MenuOutlined, QuestionCircleOutlined } from '@ant-design/icons';
import { Empty, Popover, Tooltip } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { useLanguageStore } from '../../i18n/i18n';
import { localize } from '../../i18n/language';
import { ProfileMenu, type ShellUser } from './ProfileMenu';
import { useShell } from './ShellContext';
import { ThemePopover } from './ThemePopover';

function Breadcrumbs({ crumbs }: { crumbs: string[] }) {
  const { t } = useTranslation();
  const { trail } = useShell();
  const language = useLanguageStore((s) => s.language);
  const icon = trail[0]?.icon;
  const items = [
    ...trail.map((node, index) => ({
      key: node.key,
      label: localize(node.label, language),
      to: node.path && (index < trail.length - 1 || crumbs.length > 0) ? node.path : undefined,
    })),
    ...crumbs.map((crumb, index) => ({ key: `crumb-${index}`, label: crumb, to: undefined })),
  ];

  if (items.length === 0) return <nav className="erp-crumbs" aria-label={t('shell.breadcrumb')} />;

  return (
    <nav className="erp-crumbs" aria-label={t('shell.breadcrumb')}>
      {icon && (
        <span className="erp-crumbs__icon" aria-hidden="true">
          {icon}
        </span>
      )}
      <ol>
        {items.map((item, index) => (
          <li key={item.key}>
            {item.to ? (
              <Link to={item.to}>{item.label}</Link>
            ) : (
              <span aria-current={index === items.length - 1 ? 'page' : undefined}>
                {item.label}
              </span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}

export interface TopbarProps {
  crumbs: string[];
  user: ShellUser;
  businessUnit?: ReactNode;
  notifications?: number;
  themeOpen: boolean;
  onThemeOpenChange: (open: boolean) => void;
  onShortcuts: () => void;
  onProfile?: () => void;
  onChangePassword?: () => void;
  onResetPassword?: () => void;
  onLogout?: () => void;
  onOpenMenu?: () => void;
}

export function Topbar({
  crumbs,
  user,
  businessUnit,
  notifications = 0,
  themeOpen,
  onThemeOpenChange,
  onShortcuts,
  onProfile,
  onChangePassword,
  onResetPassword,
  onLogout,
  onOpenMenu,
}: TopbarProps) {
  const { t } = useTranslation();

  return (
    <header className="erp-topbar">
      {onOpenMenu && (
        <button
          type="button"
          className="erp-tbtn"
          aria-label={t('shell.openMenu')}
          onClick={onOpenMenu}
        >
          <MenuOutlined />
        </button>
      )}
      <Breadcrumbs crumbs={crumbs} />
      <span className="erp-spacer" />
      {businessUnit}
      {businessUnit && <span className="erp-tdiv" />}
      <ThemePopover open={themeOpen} onOpenChange={onThemeOpenChange} />
      <Popover
        trigger="click"
        placement="bottomRight"
        arrow={false}
        title={t('shell.notifications')}
        content={
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={t('shell.noNotifications')}
            className="erp-notifications-empty"
          />
        }
      >
        <button
          type="button"
          className="erp-tbtn"
          aria-label={
            notifications
              ? t('shell.notificationsUnread', { count: notifications })
              : t('shell.notifications')
          }
        >
          <BellOutlined />
          {notifications > 0 && <span className="erp-tbtn__dot is-alert" />}
        </button>
      </Popover>
      <Tooltip title={t('shell.help')}>
        <button
          type="button"
          className="erp-tbtn"
          aria-label={t('shell.help')}
          onClick={onShortcuts}
        >
          <QuestionCircleOutlined />
        </button>
      </Tooltip>
      <span className="erp-tdiv" />
      <ProfileMenu
        user={user}
        onProfile={onProfile}
        onChangePassword={onChangePassword}
        onResetPassword={onResetPassword}
        onPreferences={() => onThemeOpenChange(true)}
        onShortcuts={onShortcuts}
        onLogout={onLogout}
      />
    </header>
  );
}
