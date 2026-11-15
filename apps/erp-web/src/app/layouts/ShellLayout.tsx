import { useGetErpSession, useSelectBusinessUnit, useSystemStatus } from '@erp/api-client';
import { AppShell, BusinessUnitSwitcher } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Outlet, useNavigate } from 'react-router';
import { useShallow } from 'zustand/react/shallow';
import { appConfig } from '@/common/config/app.config';
import { CHANGE_PASSWORD_PATH, FORGOT_PASSWORD_PATH } from '@/common/constants/routes';
import { footerMenu } from '@/common/navigation/footer-menu';
import { useErpNavigation } from '@/common/navigation/useErpNavigation';
import { endSession } from '@/common/services/api-client';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';

export function ShellLayout() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { user, businessUnits, currentUnit, refreshSession, selectUnit } = useAuthStore(
    useShallow((s) => ({
      user: s.user,
      businessUnits: s.businessUnits,
      currentUnit: selectCurrentUnit(s),
      refreshSession: s.refreshSession,
      selectUnit: s.selectUnit,
    })),
  );
  const { status } = useSystemStatus();
  const session = useGetErpSession({ query: { staleTime: appConfig.sessionStaleMs } });
  const select = useSelectBusinessUnit();
  const navigation = useErpNavigation();

  useEffect(() => {
    if (session.data) refreshSession(session.data);
  }, [session.data, refreshSession]);

  const switchBusinessUnit = async (businessUnitId: string) => {
    try {
      const selection = await select.mutateAsync({ data: { businessUnitId } });
      selectUnit(selection);
      await queryClient.invalidateQueries();
      void message.success(t('bu.switched', { name: selection.businessUnit.name }));
    } catch {
      void message.error(t('auth.switchFailed'));
    }
  };

  const role = currentUnit?.roleName;

  return (
    <AppShell
      workspace={{ name: user?.accountName ?? appConfig.name, subtitle: t('shell.productName') }}
      menu={navigation.menu}
      footerMenu={footerMenu}
      menuLoading={navigation.loading}
      version={status?.version}
      user={{
        name: user?.fullName ?? '',
        email: user?.email,
        role,
        tags: [
          [role, user?.isAccountAdmin ? t('shell.accountAdmin') : undefined]
            .filter(Boolean)
            .join(' · '),
          t('shell.businessUnitCount', { count: businessUnits.length }),
        ].filter(Boolean),
      }}
      businessUnit={
        <BusinessUnitSwitcher
          units={businessUnits.map((u) => ({
            id: u.id,
            code: u.code,
            name: u.name,
            description: u.roleName,
          }))}
          currentId={currentUnit?.id}
          onChange={(id) => void switchBusinessUnit(id)}
        />
      }
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
