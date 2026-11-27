import {
  BankOutlined,
  ClockCircleOutlined,
  MailOutlined,
  PhoneOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useGetErpUser, type AccessChangeDto } from '@erp/api-client';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  EmptyState,
  formatDateTime,
  StatusTag,
  type ActionItem,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { UserStatus } from '../components/UnitRoles';
import { useErpUserActions } from '../components/useErpUserActions';
import { ERP_USER_PATHS } from '../constants';

function changeText(change: AccessChangeDto) {
  const before = (change.before ? JSON.parse(change.before) : {}) as Record<string, unknown>;
  const after = (change.after ? JSON.parse(change.after) : {}) as Record<string, unknown>;
  return Object.keys(after)
    .map((key) =>
      key in before
        ? `${key}: ${String(before[key])} → ${String(after[key])}`
        : `${key}: ${String(after[key])}`,
    )
    .join(' · ');
}

export function ErpUserDetailPage() {
  const { t } = useTranslation();
  const id = useParams().id ?? '';
  const query = useGetErpUser(id);
  const { actions, modals } = useErpUserActions(() => query.refetch());
  const detail = query.data;

  if (!detail)
    return (
      <DetailsPageLayout
        loading={query.isPending}
        missing={!query.isPending}
        missingTitle={t('erpUsers.empty')}
        missingActions={[{ key: 'back', label: t('erpUsers.title'), to: ERP_USER_PATHS.list }]}
      />
    );

  const user = detail.user;
  const visible = actions(user).filter((action) => !action.hidden);
  const headerActions: ActionItem[] = [
    ...visible
      .filter((action) => !action.inMenu)
      .map((action) => ({ ...action, variant: 'primary' as const })),
    {
      key: 'more',
      label: t('common.moreActions'),
      menu: visible.filter((action) => action.inMenu),
      hidden: !visible.some((a) => a.inMenu),
    },
  ];

  const units = (
    <DetailSection
      title={t('erpUsers.tab.units')}
      description={user.isAccountAdmin ? t('erpUsers.units.adminNote') : t('erpUsers.units.note')}
    >
      {user.units.length === 0 ? (
        <EmptyState title={t('erpUsers.units.none')} />
      ) : (
        <DescriptionList
          items={user.units.map((unit) => ({
            key: unit.businessUnitId,
            label: unit.businessUnitName,
            value: (
              <>
                <StatusTag tone={unit.roleName ? 'neutral' : 'warning'}>
                  {unit.roleName ?? t('erpUsers.noRole')}
                </StatusTag>
                <StatusTag tone={unit.isActive ? 'success' : 'neutral'} dot>
                  {unit.isActive ? t('erpUsers.active') : t('erpUsers.inactive')}
                </StatusTag>
              </>
            ),
          }))}
        />
      )}
    </DetailSection>
  );

  const devices = (
    <DetailSection title={t('erpUsers.tab.devices')}>
      {detail.sessions.length === 0 ? (
        <EmptyState title={t('erpUsers.devices.none')} />
      ) : (
        <DescriptionList
          items={detail.sessions.map((session) => ({
            key: session.id,
            label: session.userAgent || '—',
            value: [
              t('erpUsers.devices.signedIn', { time: formatDateTime(session.signedInAt) }),
              t('erpUsers.devices.lastUsed', { time: formatDateTime(session.lastUsedAt) }),
              session.ipAddress,
            ]
              .filter(Boolean)
              .join(' · '),
          }))}
        />
      )}
    </DetailSection>
  );

  const logins = (
    <DetailSection title={t('erpUsers.tab.logins')}>
      {detail.logins.length === 0 ? (
        <EmptyState title={t('erpUsers.logins.none')} />
      ) : (
        <DescriptionList
          items={detail.logins.map((login, index) => ({
            key: `${login.at}-${index}`,
            label: formatDateTime(login.at),
            value: (
              <>
                <StatusTag tone={login.succeeded ? 'success' : 'danger'}>
                  {t(`erpUsers.result.${login.result}`)}
                </StatusTag>
                {login.ipAddress}
              </>
            ),
          }))}
        />
      )}
    </DetailSection>
  );

  const history = (
    <DetailSection title={t('erpUsers.tab.history')}>
      {detail.history.length === 0 ? (
        <EmptyState title={t('erpUsers.history.none')} />
      ) : (
        <DescriptionList
          items={detail.history.map((change, index) => ({
            key: `${change.at}-${index}`,
            label: [
              formatDateTime(change.at),
              change.changedBy && t('erpUsers.history.by', { name: change.changedBy }),
            ]
              .filter(Boolean)
              .join(' · '),
            value: changeText(change),
          }))}
        />
      )}
    </DetailSection>
  );

  return (
    <>
      <DetailsPageLayout
        crumbs={[user.fullName]}
        header={{
          backTo: ERP_USER_PATHS.list,
          icon: <UserOutlined />,
          title: user.fullName,
          tags: (
            <>
              <UserStatus user={user} />
              {user.isAccountAdmin && (
                <StatusTag tone="violet">{t('erpUsers.accountAdmin')}</StatusTag>
              )}
              {detail.mustChangePassword && (
                <StatusTag tone="warning">{t('erpUsers.detail.mustChange')}</StatusTag>
              )}
            </>
          ),
          meta: [
            { key: 'username', icon: <UserOutlined />, label: `@${user.username}` },
            { key: 'email', icon: <MailOutlined />, label: user.email },
            { key: 'phone', icon: <PhoneOutlined />, label: user.phone ?? '—' },
            { key: 'account', icon: <BankOutlined />, label: user.accountName },
            {
              key: 'login',
              icon: <ClockCircleOutlined />,
              label: user.lastLoginAt ? formatDateTime(user.lastLoginAt) : t('erpUsers.never'),
            },
          ],
          actions: headerActions,
        }}
        tabs={[
          {
            key: 'units',
            label: t('erpUsers.tab.units'),
            sections: [{ key: 'units', content: units }],
          },
          {
            key: 'devices',
            label: t('erpUsers.tab.devices'),
            sections: [{ key: 'devices', content: devices }],
          },
          {
            key: 'logins',
            label: t('erpUsers.tab.logins'),
            sections: [{ key: 'logins', content: logins }],
          },
          {
            key: 'history',
            label: t('erpUsers.tab.history'),
            sections: [{ key: 'history', content: history }],
          },
        ]}
      />
      {modals}
    </>
  );
}
