import { EyeOutlined } from '@ant-design/icons';
import {
  useGetAccount,
  useListAccounts,
  useListErpUsers,
  type ErpUserListItem,
} from '@erp/api-client';
import { formatDateTime, InitialsTile, ListPageLayout, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { Alert, type TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { ACCOUNT_PICKER_SIZE } from '@/modules/accounts/constants';
import { UnitRoles, UserStatus } from '../components/UnitRoles';
import { useErpUserActions } from '../components/useErpUserActions';
import { ERP_USER_PATHS, USER_TYPES, USER_VIEWS } from '../constants';

type View = (typeof USER_VIEWS)[number];
type SortField = 'fullName' | 'lastLoginAt';
type Filters = { account: string | null; unit: string | null; type: string | null };

export function ErpUserListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const list = useListState<Filters, View, SortField>({
    filters: { account: null, unit: null, type: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });
  const { account: accountFilter, unit, type, sort } = list.query;

  const query = useListErpUsers(
    {
      accountId: accountFilter ?? undefined,
      businessUnitId: unit ?? undefined,
      type: type ?? undefined,
      status: list.query.view,
      search: list.query.search || undefined,
      sort: sort ? `${sort.order === 'desc' ? '-' : ''}${sort.field}` : undefined,
      page: list.query.page,
      pageSize: list.query.pageSize,
    },
    { query: { placeholderData: keepPreviousData } },
  );
  const accounts = useListAccounts({ pageSize: ACCOUNT_PICKER_SIZE });
  const account = useGetAccount(accountFilter ?? '', { query: { enabled: !!accountFilter } });
  const { actions, modals, canChange } = useErpUserActions(() => query.refetch());
  const counts = query.data?.counts;

  const columns: TableColumnsType<ErpUserListItem> = [
    {
      key: 'fullName',
      title: t('erpUsers.col.user'),
      sorter: true,
      render: (_, user, index) => (
        <TitleCell
          leading={<InitialsTile name={user.fullName} index={index} />}
          title={user.fullName}
          subtitle={`@${user.username}`}
        />
      ),
    },
    {
      key: 'contact',
      title: t('erpUsers.col.contact'),
      render: (_, user) => <TitleCell title={user.email} subtitle={user.phone ?? '—'} />,
    },
    { key: 'account', title: t('erpUsers.col.account'), render: (_, user) => user.accountName },
    {
      key: 'units',
      title: t('erpUsers.col.units'),
      render: (_, user) => <UnitRoles user={user} />,
    },
    {
      key: 'status',
      title: t('erpUsers.col.status'),
      render: (_, user) => <UserStatus user={user} />,
    },
    {
      key: 'lastLoginAt',
      title: t('erpUsers.col.lastLogin'),
      sorter: true,
      render: (_, user) =>
        user.lastLoginAt ? formatDateTime(user.lastLoginAt) : t('erpUsers.never'),
    },
  ];

  return (
    <>
      <ListPageLayout<ErpUserListItem, Filters, View, SortField>
        subtitle={t('erpUsers.subtitle')}
        summary={!canChange && <Alert type="info" showIcon message={t('erpUsers.readOnly')} />}
        state={list}
        search={{ label: t('erpUsers.search'), placeholder: t('erpUsers.search') }}
        filters={[
          {
            key: 'account',
            label: t('erpUsers.filter.account'),
            options: (accounts.data?.items ?? []).map((a) => ({ value: a.id, label: a.name })),
          },
          {
            key: 'unit',
            label: t('erpUsers.filter.unit'),
            options: (account.data?.businessUnits ?? []).map((u) => ({
              value: u.id,
              label: u.name,
            })),
          },
          {
            key: 'type',
            label: t('erpUsers.filter.type'),
            options: USER_TYPES.map((type) => ({ value: type, label: t(`erpUsers.type.${type}`) })),
          },
        ]}
        views={USER_VIEWS.map((key) => ({
          key,
          label: t(`erpUsers.view.${key}`),
          count: key === 'locked' ? undefined : counts?.[key],
        }))}
        columns={columns}
        rowActions={(user) => [
          {
            key: 'view',
            label: t('erpUsers.action.view'),
            icon: <EyeOutlined />,
            onClick: () => navigate(ERP_USER_PATHS.detail(user.id)),
          },
          ...actions(user),
        ]}
        rows={query.data?.items}
        rowKey="id"
        total={query.data?.total}
        loading={query.isFetching}
        onRefresh={() => void query.refetch()}
        onRowClick={(user) => navigate(ERP_USER_PATHS.detail(user.id))}
        empty={{ title: t('erpUsers.empty') }}
      />
      {modals}
    </>
  );
}
