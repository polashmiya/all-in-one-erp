import {
  ApartmentOutlined,
  CheckCircleOutlined,
  DownloadOutlined,
  EditOutlined,
  LockOutlined,
  LogoutOutlined,
  PlusOutlined,
  SafetyOutlined,
  StopOutlined,
} from '@ant-design/icons';
import {
  getListUsersQueryKey,
  getUser,
  problemMessage,
  useGetRoleOptions,
  useListUsers,
  useSetUserStatus,
  useSignOutUser,
  type UserDetail,
  type UserRow,
} from '@erp/api-client';
import {
  downloadCsv,
  formatDateTime,
  ListPageLayout,
  StatusTag,
  TitleCell,
  useListState,
  withDraft,
} from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { App, Button, Tooltip, type TableColumnsType } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useListDrafts } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { listParams, useUnitKey, type StatusView } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { employeePaths, USER_FEATURE, userPaths } from '../../user-management';
import { AddUnitsModal } from '../components/AddUnitsModal';
import { AssignRoleModal } from '../components/AssignRoleModal';
import { NO_ROLE, SIGN_IN_FILTERS } from '../constants';

const VIEWS: StatusView[] = ['all', 'active', 'inactive'];

type Filters = { role: string | null; signIn: string | null };

export function UserListPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const me = useAuthStore((s) => s.user?.id);
  const access = useFeatureAccess(USER_FEATURE);
  const [assigning, setAssigning] = useState<UserRow>();
  const [adding, setAdding] = useState<UserDetail>();
  const list = useListState<Filters, StatusView>({
    filters: { role: null, signIn: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });
  const params = {
    ...listParams(list.query),
    role: list.filters.role ?? undefined,
    signIn: list.filters.signIn ?? undefined,
  };
  const query = useListUsers(params, {
    query: {
      queryKey: useUnitKey(getListUsersQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const roles = useGetRoleOptions({ query: { enabled: access.view } });
  const setStatus = useSetUserStatus();
  const signOut = useSignOutUser();
  const drafts = useListDrafts(
    USER_FEATURE,
    access.create,
    (draftId) => navigate(withDraft(userPaths.create, draftId)),
    access.view,
  );
  const data = query.data;

  const run = async (action: () => Promise<unknown>, done: string) => {
    try {
      await action();
      void message.success(t(done));
      await query.refetch();
    } catch (error) {
      void message.error(problemMessage(error, t('user.failed')));
    }
  };

  const openUnits = (row: UserRow) =>
    getUser(row.id)
      .then(setAdding)
      .catch((error: unknown) => void message.error(problemMessage(error, t('user.failed'))));

  const roleCell = (row: UserRow) => {
    if (row.isAccountAdmin) return <StatusTag tone="primary">{t('user.accountAdmin')}</StatusTag>;
    if (row.roleName) return row.roleName;
    return (
      <span className="erp-inline-stats">
        <StatusTag tone="warning">{t('user.noRole')}</StatusTag>
        {access.edit && row.id !== me && (
          <Button type="link" size="small" onClick={() => setAssigning(row)}>
            {t('user.assign')}
          </Button>
        )}
      </span>
    );
  };

  const columns: TableColumnsType<UserRow> = [
    {
      key: 'fullName',
      title: t('user.col.user'),
      sorter: true,
      render: (_, row) => (
        <TitleCell
          leading={row.isAccountAdmin ? <LockOutlined className="erp-fg--primary" /> : undefined}
          title={row.fullName}
          subtitle={`@${row.username}`}
        />
      ),
    },
    { key: 'email', title: t('user.col.email'), render: (_, row) => row.email },
    { key: 'phone', title: t('user.col.phone'), render: (_, row) => row.phone ?? '—' },
    {
      key: 'employee',
      title: t('user.col.employee'),
      render: (_, row) =>
        row.employeeId ? (
          <Link to={employeePaths.view(row.employeeId)}>{row.employeeName}</Link>
        ) : (
          '—'
        ),
    },
    { key: 'role', title: t('user.col.role'), render: (_, row) => roleCell(row) },
    {
      key: 'status',
      title: t('user.col.status'),
      render: (_, row) => (
        <span className="erp-inline-stats">
          <StatusTag tone={row.isActive ? 'success' : 'neutral'} dot>
            {row.isActive ? t('user.active') : t('user.inactive')}
          </StatusTag>
          {row.isLocked && (
            <Tooltip title={t('user.lockedHint')}>
              <StatusTag tone="danger">{t('user.locked')}</StatusTag>
            </Tooltip>
          )}
        </span>
      ),
    },
    {
      key: 'lastLoginAt',
      title: t('user.col.lastSignIn'),
      sorter: true,
      render: (_, row) => (row.lastLoginAt ? formatDateTime(row.lastLoginAt) : t('user.never')),
    },
  ];

  const exportRows = () =>
    downloadCsv(t('user.exportFile'), data?.items ?? [], [
      { header: t('user.col.user'), value: (r) => r.fullName },
      { header: t('user.col.username'), value: (r) => r.username },
      { header: t('user.col.email'), value: (r) => r.email },
      { header: t('user.col.phone'), value: (r) => r.phone },
      { header: t('user.col.employee'), value: (r) => r.employeeName },
      { header: t('user.col.role'), value: (r) => r.roleName },
      {
        header: t('user.col.status'),
        value: (r) => (r.isActive ? t('user.active') : t('user.inactive')),
      },
      { header: t('user.col.lastSignIn'), value: (r) => r.lastLoginAt },
    ]);

  return (
    <>
      <ListPageLayout<UserRow, Filters, StatusView>
        subtitle={t('user.subtitle', { unit: unit?.name ?? '' })}
        actions={[
          {
            key: 'export',
            label: t('user.export'),
            icon: <DownloadOutlined />,
            hidden: !access.export,
            onClick: exportRows,
          },
          {
            key: 'new',
            label: t('user.new'),
            icon: <PlusOutlined />,
            variant: 'primary',
            hidden: !access.create,
            to: userPaths.create,
          },
        ]}
        state={list}
        search={{ label: t('user.search'), placeholder: t('user.search') }}
        filters={[
          {
            key: 'role',
            label: t('user.col.role'),
            options: [
              { value: NO_ROLE, label: t('user.noRole') },
              ...(roles.data ?? []).map((role) => ({ value: role.id, label: role.name })),
            ],
          },
          {
            key: 'signIn',
            label: t('user.signInFilter'),
            options: SIGN_IN_FILTERS.map((value) => ({ value, label: t(`user.signIn.${value}`) })),
          },
        ]}
        views={VIEWS.map((key) => ({
          key,
          label: t(`user.view.${key}`),
          count: data?.counts[key],
        }))}
        columns={columns}
        rowActions={(row) => {
          const locked = row.isAccountAdmin || row.id === me || !access.edit;
          return [
            {
              key: 'edit',
              label: t('user.edit'),
              icon: <EditOutlined />,
              hidden: row.isAccountAdmin || !access.edit,
              onClick: () => navigate(userPaths.edit(row.id)),
            },
            {
              key: 'role',
              label: t('user.role.action'),
              icon: <SafetyOutlined />,
              inMenu: true,
              hidden: locked,
              onClick: () => setAssigning(row),
            },
            {
              key: 'units',
              label: t('user.units.action'),
              icon: <ApartmentOutlined />,
              inMenu: true,
              hidden: locked,
              onClick: () => openUnits(row),
            },
            {
              key: 'status',
              label: row.isActive ? t('user.deactivate') : t('user.activate'),
              icon: row.isActive ? <StopOutlined /> : <CheckCircleOutlined />,
              inMenu: true,
              hidden: locked,
              onClick: () =>
                run(
                  () => setStatus.mutateAsync({ id: row.id, data: { isActive: !row.isActive } }),
                  row.isActive ? 'user.deactivated' : 'user.activated',
                ),
            },
            {
              key: 'sign-out',
              label: t('user.signOut'),
              icon: <LogoutOutlined />,
              inMenu: true,
              hidden: locked,
              confirm: { title: t('user.signOutConfirm', { name: row.fullName }) },
              onClick: () => run(() => signOut.mutateAsync({ id: row.id }), 'user.signedOut'),
            },
          ];
        }}
        rows={data?.items}
        rowKey="id"
        total={data?.total}
        loading={query.isFetching}
        empty={{ title: t('user.empty') }}
        drafts={drafts}
      />
      <AssignRoleModal
        user={assigning}
        onClose={() => setAssigning(undefined)}
        onDone={() => query.refetch()}
      />
      <AddUnitsModal
        user={adding}
        onClose={() => setAdding(undefined)}
        onDone={() => query.refetch()}
      />
    </>
  );
}
