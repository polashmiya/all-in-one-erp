import {
  useListConfigurationChanges,
  useListLookupTypes,
  type ConfigurationChangeRow,
} from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { formatDateTime, ListPageLayout, StatusTag, TitleCell, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import type { TableColumnsType } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useAccountScreens } from '../../components/account-screens';
import { ChangeDrawer } from '../components/ChangeDrawer';
import { HISTORY_AREAS } from '../constants';
import { changedValues } from '../history';

type Filters = { feature: string | null; unit: string | null; area: string | null };

export function AccountHistoryPage() {
  const { t } = useTranslation();
  const text = useText();
  const accountId = useParams().id ?? '';
  const { account, features, units } = useAccountScreens(accountId);
  const lookupTypes = useListLookupTypes();
  const [open, setOpen] = useState<ConfigurationChangeRow>();
  const list = useListState<Filters>({
    filters: { feature: null, unit: null, area: null },
    pageSize: appConfig.defaultPageSize,
  });

  const query = useListConfigurationChanges(
    accountId,
    {
      feature: list.filters.feature ?? undefined,
      businessUnitId: list.filters.unit ?? undefined,
      area: list.filters.area ?? undefined,
      page: list.query.page,
      pageSize: list.query.pageSize,
    },
    { query: { placeholderData: keepPreviousData } },
  );

  const screenOf = (row: ConfigurationChangeRow) =>
    row.feature
      ? (features.find((f) => f.value === row.feature)?.label ?? row.feature)
      : (text(lookupTypes.data?.find((type) => type.key === row.key)?.name) ?? row.key);
  const titleOf = (row: ConfigurationChangeRow) =>
    `${t(`history.area.${row.area}`)} · ${row.feature ? row.key : screenOf(row)}`;

  const columns: TableColumnsType<ConfigurationChangeRow> = [
    {
      key: 'at',
      title: t('history.col.when'),
      render: (_, row) => (
        <TitleCell title={formatDateTime(row.at)} subtitle={row.userName ?? '—'} />
      ),
    },
    { key: 'screen', title: t('config.screen'), render: (_, row) => screenOf(row) },
    {
      key: 'scope',
      title: t('history.col.scope'),
      render: (_, row) => row.businessUnitName ?? t('history.wholeAccount'),
    },
    {
      key: 'what',
      title: t('history.col.what'),
      render: (_, row) => (
        <>
          <StatusTag tone="violet">{t(`history.area.${row.area}`)}</StatusTag>{' '}
          {row.feature ? row.key : null}
        </>
      ),
    },
    {
      key: 'changes',
      title: t('history.col.changes'),
      align: 'end',
      render: (_, row) => changedValues(row.before, row.after).length,
    },
  ];

  return (
    <>
      <ListPageLayout<ConfigurationChangeRow, Filters, string>
        title={t('history.title', { account: account.data?.name ?? '' })}
        subtitle={t('history.subtitle')}
        state={list}
        filters={[
          { key: 'feature', label: t('config.screen'), options: features },
          { key: 'unit', label: t('config.unit'), options: units },
          {
            key: 'area',
            label: t('history.col.what'),
            options: HISTORY_AREAS.map((area) => ({
              value: area,
              label: t(`history.area.${area}`),
            })),
          },
        ]}
        columns={columns}
        rows={query.data?.items}
        rowKey="id"
        total={query.data?.total}
        loading={account.isPending || query.isFetching}
        onRefresh={() => void query.refetch()}
        onRowClick={setOpen}
        empty={{ title: t('history.empty') }}
      />
      <ChangeDrawer
        change={open}
        title={open ? titleOf(open) : ''}
        onClose={() => setOpen(undefined)}
      />
    </>
  );
}
