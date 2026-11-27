import {
  CheckCircleOutlined,
  DeleteOutlined,
  DownloadOutlined,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  SafetyOutlined,
  StopOutlined,
} from '@ant-design/icons';
import {
  getListRolesQueryKey,
  problemMessage,
  useDeleteRole,
  useListRoles,
  useSetRoleStatus,
  type RoleRow,
} from '@erp/api-client';
import {
  downloadCsv,
  ListPageLayout,
  StatusTag,
  TitleCell,
  useListState,
  withDraft,
} from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { App, Progress, type TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useListDrafts } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { listParams, useUnitKey, type StatusView } from '@/common/master-data';
import { USER_ROLE_FEATURE } from '../../user-management';
import { rolePaths } from '../constants';

const VIEWS: StatusView[] = ['all', 'active', 'inactive'];

export function RoleListPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const access = useFeatureAccess(USER_ROLE_FEATURE);
  const list = useListState<Record<string, never>, StatusView>({
    filters: {},
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });
  const params = listParams(list.query);
  const query = useListRoles(params, {
    query: {
      queryKey: useUnitKey(getListRolesQueryKey(params)),
      placeholderData: keepPreviousData,
    },
  });
  const setStatus = useSetRoleStatus();
  const remove = useDeleteRole();
  const drafts = useListDrafts(USER_ROLE_FEATURE, access.create, (draftId) =>
    navigate(withDraft(rolePaths.create, draftId)),
  );
  const data = query.data;

  const run = async (action: () => Promise<unknown>, done: string) => {
    try {
      await action();
      void message.success(t(done));
      await query.refetch();
    } catch (error) {
      void message.error(problemMessage(error, t('role.failed')));
    }
  };

  const permissionsText = (role: RoleRow) =>
    role.isAdministrator
      ? t('role.all')
      : t('role.permissionsOf', {
          count: role.permissionCount,
          total: data?.totalPermissions ?? 0,
        });

  const columns: TableColumnsType<RoleRow> = [
    {
      key: 'name',
      title: t('role.col.role'),
      sorter: true,
      render: (_, role) => (
        <TitleCell
          leading={<SafetyOutlined className="erp-fg--primary" />}
          title={role.name}
          subtitle={role.isSystem ? t('role.system') : t('role.custom')}
        />
      ),
    },
    {
      key: 'description',
      title: t('role.col.description'),
      render: (_, role) => role.description ?? '—',
    },
    {
      key: 'permissions',
      title: t('role.col.permissions'),
      render: (_, role) => (
        <span className="erp-inline-stats">
          <Progress
            percent={
              data?.totalPermissions ? (role.permissionCount / data.totalPermissions) * 100 : 0
            }
            showInfo={false}
            size="small"
            className="role-progress"
          />
          {permissionsText(role)}
        </span>
      ),
    },
    { key: 'users', title: t('role.col.users'), render: (_, role) => role.usersInUnit },
    {
      key: 'status',
      title: t('role.col.status'),
      render: (_, role) => (
        <StatusTag tone={role.isActive ? 'success' : 'neutral'} dot>
          {role.isActive ? t('role.active') : t('role.inactive')}
        </StatusTag>
      ),
    },
  ];

  const exportRows = () =>
    downloadCsv(t('role.exportFile'), data?.items ?? [], [
      { header: t('role.col.role'), value: (r) => r.name },
      { header: t('role.col.description'), value: (r) => r.description },
      { header: t('role.col.permissions'), value: permissionsText },
      { header: t('role.col.users'), value: (r) => r.usersInUnit },
      {
        header: t('role.col.status'),
        value: (r) => (r.isActive ? t('role.active') : t('role.inactive')),
      },
    ]);

  return (
    <ListPageLayout<RoleRow, Record<string, never>, StatusView>
      subtitle={t('role.subtitle')}
      actions={[
        {
          key: 'export',
          label: t('role.export'),
          icon: <DownloadOutlined />,
          hidden: !access.export,
          onClick: exportRows,
        },
        {
          key: 'new',
          label: t('role.new'),
          icon: <PlusOutlined />,
          variant: 'primary',
          hidden: !access.create,
          to: rolePaths.create,
        },
      ]}
      state={list}
      search={{ label: t('role.search'), placeholder: t('role.search') }}
      views={VIEWS.map((key) => ({ key, label: t(`role.view.${key}`), count: data?.counts[key] }))}
      columns={columns}
      rowActions={(role) => [
        {
          key: 'open',
          label: role.isAdministrator || !access.edit ? t('role.viewAction') : t('role.edit'),
          icon: role.isAdministrator || !access.edit ? <EyeOutlined /> : <EditOutlined />,
          onClick: () => navigate(rolePaths.edit(role.id)),
        },
        {
          key: 'status',
          label: role.isActive ? t('role.deactivate') : t('role.activate'),
          icon: role.isActive ? <StopOutlined /> : <CheckCircleOutlined />,
          inMenu: true,
          hidden: role.isAdministrator || !access.edit,
          onClick: () =>
            run(
              () => setStatus.mutateAsync({ id: role.id, data: { isActive: !role.isActive } }),
              role.isActive ? 'role.deactivated' : 'role.activated',
            ),
        },
        {
          key: 'delete',
          label: t('role.delete'),
          icon: <DeleteOutlined />,
          inMenu: true,
          danger: true,
          hidden: role.isSystem || role.usersInUnit > 0 || !access.delete,
          confirm: { title: t('role.deleteConfirm', { name: role.name }), danger: true },
          onClick: () => run(() => remove.mutateAsync({ id: role.id }), 'role.deleted'),
        },
      ]}
      rows={data?.items}
      rowKey="id"
      total={data?.total}
      loading={query.isFetching}
      onRowClick={(role) => navigate(rolePaths.edit(role.id))}
      empty={{ title: t('role.empty') }}
      drafts={drafts}
    />
  );
}
