import { ApartmentOutlined, TableOutlined } from '@ant-design/icons';
import {
  getListChartOfAccountsQueryKey,
  listChartOfAccounts,
  useListChartOfAccounts,
  useSetChartOfAccountsStatus,
  type ChartOfAccountRow,
} from '@erp/api-client';
import { SoftTag, useDisclosure, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { Segmented } from 'antd';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useRefreshDocuments } from '@/common/documents/documents';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import {
  EXPORT_PAGE_SIZE,
  idParam,
  listParams,
  MasterListPage,
  useMasterColumns,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import { useAuthStore } from '@/common/store/auth-store';
import { useAccountOptions, useAccountTypes } from '../../../accounting';
import { useChartLayout, type ChartLayout } from '../chart-layout';
import { AccountFormModal } from '../components/AccountFormModal';
import { AccountTree } from '../components/AccountTree';
import { CHART_FEATURE } from '../constants';

type Filters = { type: string | null; parentId: string | null };

export function ChartOfAccountsPage() {
  const { t } = useTranslation();
  const accountName = useAuthStore((s) => s.user?.accountName ?? '');
  const access = useFeatureAccess(CHART_FEATURE);
  const config = useFeatureConfig(CHART_FEATURE, access.view);
  const modal = useDisclosure<{ account?: ChartOfAccountRow; parent?: ChartOfAccountRow }>();
  const { layout, setLayout } = useChartLayout();
  const refresh = useRefreshDocuments();
  const types = useAccountTypes();
  const accounts = useAccountOptions(access.view);
  const list = useListState<Filters, StatusView>({
    filters: { type: null, parentId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams(list.query),
    type: list.filters.type ?? undefined,
    parentId: idParam(list.filters.parentId),
  };
  const query = useListChartOfAccounts(params, {
    query: {
      queryKey: useUnitKey(getListChartOfAccountsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const treeParams = { page: 1, pageSize: EXPORT_PAGE_SIZE };
  const tree = useListChartOfAccounts(treeParams, {
    query: {
      queryKey: useUnitKey(getListChartOfAccountsQueryKey(treeParams)),
      enabled: access.view && layout === 'tree',
    },
  });
  const setStatus = useSetChartOfAccountsStatus();
  const filters = useConfigFilters<keyof Filters>(config.data, {
    type: types,
    parentId: accounts?.filter((a) => a.isGroup),
  });

  const edit = access.edit ? (row: ChartOfAccountRow) => modal.onOpen({ account: row }) : undefined;
  const typeName = (row: ChartOfAccountRow) => t(`accounting.type.${row.type}`);
  const { columns, csvColumns } = useMasterColumns<ChartOfAccountRow>(config.data, {
    onOpen: edit,
    renderers: {
      code: { render: (row) => <SoftTag>{row.code}</SoftTag> },
      type: { render: typeName, csv: typeName },
    },
  });

  return (
    <MasterListPage<ChartOfAccountRow, Filters>
      feature={CHART_FEATURE}
      title={t('chart.title')}
      subtitle={t('chart.subtitle', { account: accountName })}
      newLabel={t('chart.new')}
      search={{ label: t('chart.search'), placeholder: t('chart.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || !config.data}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('chart.exportFile')}
      fetchAll={() =>
        listChartOfAccounts(layout === 'tree' ? treeParams : { ...params, ...treeParams }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('chart.emptyTitle'), description: t('chart.emptyText') }}
      extra={
        <Segmented<ChartLayout>
          aria-label={t('chart.layout')}
          value={layout}
          onChange={setLayout}
          options={[
            { value: 'tree', label: t('chart.tree'), icon: <ApartmentOutlined /> },
            { value: 'table', label: t('chart.table'), icon: <TableOutlined /> },
          ]}
        />
      }
      body={
        layout === 'tree' && (
          <AccountTree
            accounts={tree.data?.items}
            search={list.search}
            onAdd={(parent) => modal.onOpen({ parent })}
            onEdit={(account) => modal.onOpen({ account })}
          />
        )
      }
      onCreate={() => modal.onOpen()}
      onEdit={(row) => modal.onOpen({ account: row })}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
    >
      {config.data && (
        <AccountFormModal
          config={config.data}
          open={modal.open}
          account={modal.data?.account}
          parent={modal.data?.parent}
          accounts={accounts}
          accountName={accountName}
          onClose={modal.onClose}
          onSaved={refresh}
        />
      )}
    </MasterListPage>
  );
}
