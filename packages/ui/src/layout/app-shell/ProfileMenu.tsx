import {
  DownOutlined,
  KeyOutlined,
  LockOutlined,
  LogoutOutlined,
  MailOutlined,
  SettingOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { Dropdown, Flex, Typography } from 'antd';
import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { StatusTag } from '../../components/data-display/StatusTag';
import { LanguageSegmented } from '../../components/navigation/LanguageSegmented';
import { initials } from '../../utils/format';

export interface ShellUser {
  name: string;
  email?: string;
  role?: string;
  tags?: string[];
  avatarUrl?: string;
}

export function UserAvatar({
  user,
  large = false,
  online = false,
}: {
  user: ShellUser;
  large?: boolean;
  online?: boolean;
}) {
  return (
    <span className={`erp-avatar${large ? ' is-lg' : ''}`} aria-hidden="true">
      {user.avatarUrl ? <img src={user.avatarUrl} alt="" /> : initials(user.name)}
      {online && <span className="erp-avatar__online" />}
    </span>
  );
}

function MenuButton({
  icon,
  label,
  hint,
  danger,
  onClick,
}: {
  icon: ReactNode;
  label: string;
  hint?: string;
  danger?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      className={`erp-menu-item${danger ? ' is-danger' : ''}`}
      onClick={onClick}
    >
      <span className="erp-menu-item__icon">{icon}</span>
      <span>{label}</span>
      {hint && <span className="erp-menu-item__hint">{hint}</span>}
    </button>
  );
}

export interface ProfileMenuProps {
  user: ShellUser;
  onProfile?: () => void;
  onPreferences: () => void;
  onShortcuts: () => void;
  onChangePassword?: () => void;
  onResetPassword?: () => void;
  onLogout?: () => void;
}

export function ProfileMenu({
  user,
  onProfile,
  onPreferences,
  onShortcuts,
  onChangePassword,
  onResetPassword,
  onLogout,
}: ProfileMenuProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const run = (action?: () => void) => () => {
    setOpen(false);
    action?.();
  };

  const panel = (
    <div className="erp-popover erp-profile-menu">
      <Flex gap={12} align="center" className="erp-profile-menu__head">
        <UserAvatar user={user} large />
        <div className="erp-stack is-tight">
          <Typography.Text strong ellipsis>
            {user.name}
          </Typography.Text>
          {user.email && (
            <Typography.Text type="secondary" ellipsis className="erp-text-sm">
              {user.email}
            </Typography.Text>
          )}
        </div>
      </Flex>
      {user.tags && user.tags.length > 0 && (
        <Flex gap={6} wrap className="erp-profile-menu__tags">
          {user.tags.map((tag, index) => (
            <StatusTag key={tag} tone={index === 0 ? 'primary' : 'neutral'}>
              {tag}
            </StatusTag>
          ))}
        </Flex>
      )}
      <div className="erp-menu-sep" />
      {onProfile && (
        <MenuButton icon={<UserOutlined />} label={t('shell.profile')} onClick={run(onProfile)} />
      )}
      <MenuButton
        icon={<SettingOutlined />}
        label={t('shell.preferences')}
        onClick={run(onPreferences)}
      />
      {onChangePassword && (
        <MenuButton
          icon={<LockOutlined />}
          label={t('shell.changePassword')}
          onClick={run(onChangePassword)}
        />
      )}
      {onResetPassword && (
        <MenuButton
          icon={<MailOutlined />}
          label={t('shell.resetPassword')}
          onClick={run(onResetPassword)}
        />
      )}
      <MenuButton
        icon={<KeyOutlined />}
        label={t('shell.shortcuts')}
        hint="?"
        onClick={run(onShortcuts)}
      />
      <Flex align="center" justify="space-between" className="erp-profile-menu__row">
        <span>{t('shell.language')}</span>
        <LanguageSegmented size="small" />
      </Flex>
      {onLogout && (
        <>
          <div className="erp-menu-sep" />
          <MenuButton
            icon={<LogoutOutlined />}
            label={t('shell.logout')}
            danger
            onClick={run(onLogout)}
          />
        </>
      )}
    </div>
  );

  return (
    <Dropdown
      trigger={['click']}
      placement="bottomRight"
      open={open}
      onOpenChange={setOpen}
      popupRender={() => panel}
    >
      <button
        type="button"
        className="erp-profile"
        aria-label={t('shell.accountMenu', { name: user.name })}
        aria-expanded={open}
      >
        <UserAvatar user={user} online />
        <span className="erp-profile__text">
          <span className="erp-profile__name">{user.name}</span>
          {user.role && <span className="erp-profile__role">{user.role}</span>}
        </span>
        <span className="erp-profile__chev">
          <DownOutlined />
        </span>
      </button>
    </Dropdown>
  );
}
