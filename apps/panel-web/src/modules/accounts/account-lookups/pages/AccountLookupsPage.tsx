import { CheckCircleOutlined, EditOutlined, PlusOutlined, StopOutlined } from '@ant-design/icons';
import {
  getListAccountLookupValuesQueryKey,
  useGetAccount,
  useListAccountLookupValues,
  useListLookupTypes,
  useUpdateLookupValue,
  type LookupValueRow,
} from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { ListPageLayout, StatusTag, TitleCell, useListState, useUndoMessage } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { App, type TableColumnsType } from 'antd';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { LookupValueModal } from '../components/LookupValueModal';

type Filters = { list: string | null };
type View = 'all' | 'active' | 'inactive';

export function AccountLookupsPage() {
  const { t } = useTranslation();
  const text = useText();
  const { message } = App.useApp();
  const showUndo = useUndoMessage();
  const accountId = useParams().id ?? '';
  const queryClient = useQueryClient();
  const account = useGetAccount(accountId);
  const types = useListLookupTypes();
  const update = useUpdateLookupValue();
  const [editing, setEditing] = useState<{ value?: LookupValueRow }>();
  const list = useListState<Filters, View>({
    filters: { list: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const firstType = types.data?.[0]?.key;
  const { setFilter } = list;
  useEffect(() => {
    if (!list.filters.list && firstType) setFilter('list', firstType);
  }, [list.filters.list, firstType, setFilter]);

  const type = list.filters.list ?? firstType ?? '';
  const listName = text(types.data?.find((ty) => ty.key === type)?.name) ?? type;
  const values = useListAccountLookupValues(accountId, type, { query: { enabled: !!type } });
  const refresh = () =>
    queryClient.invalidateQueries({
      queryKey: getListAccountLookupValuesQueryKey(accountId, type),
    });

  const all = values.data ?? [];
  const search = list.query.search.trim().toLowerCase();
  const rows = all.filter(
    (value) =>
      (list.view === 'all' || value.isActive === (list.view === 'active')) &&
      (!search ||
        [value.code, value.nameEn, value.nameBn].some((part) =>
          part?.toLowerCase().includes(search),
        )),
  );
  const counts = {
    all: all.length,
    active: all.filter((v) => v.isActive).length,
    inactive: all.filter((v) => !v.isActive).length,
  };

  const setActive = async (value: LookupValueRow, isActive: boolean, undoable = true) => {
    try {
      await update.mutateAsync({
        id: accountId,
        type,
        valueId: value.id,
        data: {
          code: value.code,
          nameEn: value.nameEn,
          nameBn: value.nameBn,
          sortOrder: value.sortOrder,
          isDefault: isActive && value.isDefault,
          isActive,
        },
      });
      await refresh();
      const done = t(isActive ? 'lookups.activated' : 'lookups.deactivated');
      if (undoable) showUndo(done, () => void setActive(value, !isActive, false));
      else void message.success(done);
    } catch {
      void message.error(t('lookups.failed'));
    }
  };

  const columns: TableColumnsType<LookupValueRow> = [
    {
      key: 'name',
      title: t('lookups.col.name'),
      render: (_, value) => <TitleCell title={value.nameEn} subtitle={value.code ?? undefined} />,
    },
    {
      key: 'nameBn',
      title: t('lookups.col.nameBn'),
      render: (_, value) => value.nameBn ?? '—',
    },
    { key: 'order', title: t('lookups.col.order'), align: 'end', render: (_, v) => v.sortOrder },
    {
      key: 'default',
      title: t('lookups.col.default'),
      render: (_, value) =>
        value.isDefault ? <StatusTag tone="info">{t('lookups.default')}</StatusTag> : '—',
    },
    {
      key: 'status',
      title: t('lookups.col.status'),
      render: (_, value) => (
        <StatusTag tone={value.isActive ? 'success' : 'neutral'} dot>
          {value.isActive ? t('accounts.status.active') : t('accounts.status.inactive')}
        </StatusTag>
      ),
    },
  ];

  return (
    <>
      <ListPageLayout<LookupValueRow, Filters, View>
        title={t('lookups.title', { account: account.data?.name ?? '' })}
        subtitle={t('lookups.subtitle')}
        actions={[
          {
            key: 'new',
            label: t('lookups.new'),
            icon: <PlusOutlined />,
            variant: 'primary',
            disabled: !type,
            onClick: () => setEditing({}),
          },
        ]}
        state={list}
        search={{ label: t('lookups.search'), placeholder: t('lookups.search') }}
        filters={[
          {
            key: 'list',
            label: t('lookups.list'),
            options: (types.data ?? []).map((ty) => ({
              value: ty.key,
              label: text(ty.name) ?? ty.key,
            })),
          },
        ]}
        views={(['all', 'active', 'inactive'] as const).map((key) => ({
          key,
          label: t(`accounts.tab.${key}`),
          count: counts[key],
        }))}
        columns={columns}
        rows={rows}
        rowKey="id"
        total={rows.length}
        loading={account.isPending || values.isFetching}
        onRefresh={() => void values.refetch()}
        onRowClick={(value) => setEditing({ value })}
        rowActions={(value) => [
          {
            key: 'edit',
            label: t('lookups.edit'),
            icon: <EditOutlined />,
            onClick: () => setEditing({ value }),
          },
          {
            key: 'status',
            label: value.isActive ? t('lookups.deactivate') : t('lookups.activate'),
            icon: value.isActive ? <StopOutlined /> : <CheckCircleOutlined />,
            inMenu: true,
            onClick: () => setActive(value, !value.isActive),
          },
        ]}
        empty={{ title: t('lookups.empty') }}
      />
      <LookupValueModal
        key={editing?.value?.id ?? 'new'}
        accountId={accountId}
        type={type}
        listName={listName}
        open={!!editing}
        value={editing?.value}
        nextOrder={Math.max(0, ...all.map((v) => v.sortOrder))}
        onClose={() => setEditing(undefined)}
        onSaved={refresh}
      />
    </>
  );
}
