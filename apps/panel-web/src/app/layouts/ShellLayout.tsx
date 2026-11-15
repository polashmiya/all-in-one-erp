import { useGetPanelSession, useSystemStatus } from '@erp/api-client';
import { AppShell } from '@erp/ui';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Outlet, useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { CHANGE_PASSWORD_PATH, FORGOT_PASSWORD_PATH } from '@/common/constants/routes';
import { useNavigation } from '@/common/navigation/useNavigation';
import { endSession } from '@/common/services/api-client';
import { useAuthStore } from '@/common/store/auth-store';

export function ShellLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);
  const { status } = useSystemStatus();
  const session = useGetPanelSession({ query: { staleTime: appConfig.sessionStaleMs } });
  const navigation = useNavigation();

  useEffect(() => {
    if (session.data) setUser(session.data);
  }, [session.data, setUser]);

  const role = user ? t(`role.${user.role}`) : undefined;

  return (
    <AppShell
      workspace={{ name: appConfig.name, subtitle: t('shell.panelSubtitle') }}
      menu={navigation.menu}
      menuLoading={navigation.loading}
      version={status?.version}
      user={{
        name: user?.fullName ?? '',
        email: user?.email,
        role,
        tags: role ? [role] : undefined,
      }}
      onChangePassword={() => navigate(CHANGE_PASSWORD_PATH)}
      onResetPassword={() =>
        navigate(`${FORGOT_PASSWORD_PATH}?login=${encodeURIComponent(user?.email ?? '')}`)
      }
      onLogout={endSession}
    >
      <Outlet />
    </AppShell>
  );
}
