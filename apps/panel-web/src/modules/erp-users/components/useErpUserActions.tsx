import {
  CheckCircleOutlined,
  EditOutlined,
  KeyOutlined,
  LogoutOutlined,
  StopOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import {
  useSetErpUserStatus,
  useSignOutErpUser,
  useUnlockErpUser,
  type ErpUserListItem,
} from '@erp/api-client';
import type { RowAction } from '@erp/ui';
import { App } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/common/store/auth-store';
import { SUPER_ADMIN_ROLE } from '../constants';
import { ErpUserEditModal } from './ErpUserEditModal';
import { ResetPasswordModal } from './ResetPasswordModal';

export function useIsSuperAdmin() {
  return useAuthStore((s) => s.user?.role === SUPER_ADMIN_ROLE);
}

export function useErpUserActions(onChanged: () => unknown) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const canChange = useIsSuperAdmin();
  const [editing, setEditing] = useState<ErpUserListItem>();
  const [resetting, setResetting] = useState<ErpUserListItem>();
  const setStatus = useSetErpUserStatus();
  const unlock = useUnlockErpUser();
  const signOut = useSignOutErpUser();

  const run = async (action: () => Promise<unknown>, done: string) => {
    try {
      await action();
      void message.success(t(done));
      await onChanged();
    } catch {
      void message.error(t('erpUsers.failed'));
    }
  };

  const actions = (user: ErpUserListItem): RowAction[] => [
    {
      key: 'edit',
      label: t('erpUsers.action.edit'),
      icon: <EditOutlined />,
      hidden: !canChange,
      onClick: () => setEditing(user),
    },
    {
      key: 'reset',
      label: t('erpUsers.action.reset'),
      icon: <KeyOutlined />,
      inMenu: true,
      hidden: !canChange,
      onClick: () => setResetting(user),
    },
    {
      key: 'unlock',
      label: t('erpUsers.action.unlock'),
      icon: <UnlockOutlined />,
      inMenu: true,
      hidden: !canChange || !user.isLocked,
      onClick: () => run(() => unlock.mutateAsync({ id: user.id }), 'erpUsers.done.unlocked'),
    },
    {
      key: 'status',
      label: user.isActive ? t('erpUsers.action.deactivate') : t('erpUsers.action.activate'),
      icon: user.isActive ? <StopOutlined /> : <CheckCircleOutlined />,
      inMenu: true,
      hidden: !canChange,
      confirm: user.isActive
        ? {
            title: t('erpUsers.confirm.deactivate', { name: user.fullName }),
            description: t('erpUsers.confirm.deactivateText'),
            danger: true,
          }
        : undefined,
      onClick: () =>
        run(
          () => setStatus.mutateAsync({ id: user.id, data: { isActive: !user.isActive } }),
          user.isActive ? 'erpUsers.done.deactivated' : 'erpUsers.done.activated',
        ),
    },
    {
      key: 'signOut',
      label: t('erpUsers.action.signOut'),
      icon: <LogoutOutlined />,
      inMenu: true,
      danger: true,
      hidden: !canChange,
      confirm: { title: t('erpUsers.confirm.signOut', { name: user.fullName }), danger: true },
      onClick: () => run(() => signOut.mutateAsync({ id: user.id }), 'erpUsers.done.signedOut'),
    },
  ];

  const modals = (
    <>
      <ErpUserEditModal user={editing} onClose={() => setEditing(undefined)} onSaved={onChanged} />
      <ResetPasswordModal
        user={resetting}
        onClose={() => setResetting(undefined)}
        onDone={onChanged}
      />
    </>
  );

  return { actions, modals, canChange };
}
