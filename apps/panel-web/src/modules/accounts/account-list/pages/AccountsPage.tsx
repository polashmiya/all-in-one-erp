import {
  ApartmentOutlined,
  AppstoreOutlined,
  BankOutlined,
  EyeOutlined,
  InboxOutlined,
  PlusOutlined,
  ShopOutlined,
} from '@ant-design/icons';
import { useListAccounts, type AccountListItem } from '@erp/api-client';
import {
  DotValue,
  formatDate,
  formatNumber,
  InitialsTile,
  ListPageLayout,
  StatGrid,
  StatusTag,
  TitleCell,
  useLanguageStore,
  useListState,
} from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import type { TableColumnsType } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { ACCOUNT_PATHS } from '../../constants';
import { countryName } from '../../utils/format';
import { AccountDetailDrawer } from '../components/AccountDetailDrawer';

type View = 'all' | 'active' | 'inactive';
type SortField = 'name' | 'createdAt';

export function AccountsPage() {
  const { t } = useTranslation();
  const language = useLanguageStore((s) => s.language);
  const [selectedId, setSelectedId] = useState<string>();
  const list = useListState<Record<string, never>, View, SortField>({
    filters: {},
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const { sort } = list.query;
  const query = useListAccounts(
    {
      search: list.query.search || undefined,
      status: list.query.view,
      sort: sort ? `${sort.order === 'desc' ? '-' : ''}${sort.field}` : undefined,
      page: list.query.page,
      pageSize: list.query.pageSize,
    },
    { query: { placeholderData: keepPreviousData } },
  );
  const result = query.data;
  const summary = result?.summary;

  const columns: TableColumnsType<AccountListItem> = [
    {
      key: 'name',
      title: t('accounts.col.account'),
      sorter: true,
      render: (_, account, index) => (
        <TitleCell
          leading={<InitialsTile name={account.name} index={index} />}
          title={account.name}
          subtitle={
            <span className="erp-inline-stats">
              <span>{account.code}</span>
              <StatusTag tone={account.isActive ? 'success' : 'neutral'} dot>
                {account.isActive ? t('accounts.status.active') : t('accounts.status.inactive')}
              </StatusTag>
            </span>
          }
        />
      ),
    },
    {
      key: 'contact',
      title: t('accounts.col.contact'),
      render: (_, account) => (
        <TitleCell title={account.email ?? '—'} subtitle={account.phone ?? '—'} />
      ),
    },
    {
      key: 'location',
      title: t('accounts.col.location'),
      render: (_, account) => (
        <TitleCell title={account.city ?? '—'} subtitle={countryName(account.country, language)} />
      ),
    },
    {
      key: 'structure',
      title: t('accounts.col.structure'),
      render: (_, account) => (
        <span className="erp-inline-stats">
          <DotValue tone="primary" label={t('accounts.stat.businessUnits')}>
            <ApartmentOutlined /> {account.businessUnits}
          </DotValue>
          <DotValue tone="info" label={t('accounts.stat.offices')}>
            <ShopOutlined /> {account.offices}
          </DotValue>
          <DotValue tone="warning" label={t('accounts.stat.warehouses')}>
            <InboxOutlined /> {account.warehouses}
          </DotValue>
          <DotValue label={t('menus.title')}>
            <AppstoreOutlined /> {account.features}
          </DotValue>
        </span>
      ),
    },
    {
      key: 'admin',
      title: t('accounts.col.admin'),
      ellipsis: true,
      render: (_, account) => account.adminEmail ?? '—',
    },
    {
      key: 'createdAt',
      title: t('accounts.col.created'),
      sorter: true,
      render: (_, account) => formatDate(account.createdAt),
    },
  ];

  return (
    <>
      <ListPageLayout<AccountListItem, Record<string, never>, View, SortField>
        subtitle={t('accounts.subtitle')}
        actions={[
          {
            key: 'new',
            label: t('accounts.new'),
            icon: <PlusOutlined />,
            variant: 'primary',
            to: ACCOUNT_PATHS.create,
          },
        ]}
        summary={
          <StatGrid
            items={[
              {
                key: 'accounts',
                icon: <BankOutlined />,
                label: t('accounts.stat.accounts'),
                value: formatNumber(summary?.accounts),
                caption: summary
                  ? t('accounts.stat.active', { count: summary.activeAccounts })
                  : undefined,
              },
              {
                key: 'units',
                icon: <ApartmentOutlined />,
                tint: 'blue',
                label: t('accounts.stat.businessUnits'),
                value: formatNumber(summary?.businessUnits),
              },
              {
                key: 'offices',
                icon: <ShopOutlined />,
                tint: 'violet',
                label: t('accounts.stat.offices'),
                value: formatNumber(summary?.offices),
              },
              {
                key: 'warehouses',
                icon: <InboxOutlined />,
                tint: 'amber',
                label: t('accounts.stat.warehouses'),
                value: formatNumber(summary?.warehouses),
              },
            ]}
          />
        }
        state={list}
        search={{ label: t('accounts.search'), placeholder: t('accounts.search') }}
        views={(['all', 'active', 'inactive'] as const).map((key) => ({
          key,
          label: t(`accounts.tab.${key}`),
        }))}
        columns={columns}
        rowActions={(account) => [
          {
            key: 'view',
            label: t('accounts.view'),
            icon: <EyeOutlined />,
            onClick: () => setSelectedId(account.id),
          },
        ]}
        rows={result?.items}
        rowKey="id"
        total={result?.total}
        loading={query.isFetching}
        onRefresh={() => void query.refetch()}
        onRowClick={(account) => setSelectedId(account.id)}
        empty={{
          title: t('accounts.empty'),
          actions: [
            {
              key: 'new',
              label: t('accounts.setupFirst'),
              icon: <PlusOutlined />,
              variant: 'primary',
              to: ACCOUNT_PATHS.create,
            },
          ],
        }}
      />
      <AccountDetailDrawer accountId={selectedId} onClose={() => setSelectedId(undefined)} />
    </>
  );
}
