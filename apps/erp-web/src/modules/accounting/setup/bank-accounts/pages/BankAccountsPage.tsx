import {
  getListBankAccountsQueryKey,
  listBankAccounts,
  useListBankAccounts,
  useSetBankAccountsStatus,
  type BankAccountRow,
} from '@erp/api-client';
import type { LookupItem } from '@erp/metadata-engine';
import { useDisclosure, useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { EyeOutlined } from '@ant-design/icons';
import { appConfig } from '@/common/config/app.config';
import { useRefreshDocuments } from '@/common/documents/documents';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import {
  EXPORT_PAGE_SIZE,
  listParams,
  MasterListPage,
  useMasterColumns,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import { NO_DATES } from '@/common/reports/report';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  ACCOUNT_LEDGER_FEATURE,
  accountLedgerLink,
} from '../../../reports/account-ledger/constants';
import { BankAccountFormModal } from '../components/BankAccountFormModal';
import { BANK_FEATURE, BANK_TYPES } from '../constants';

type Filters = { type: string | null };

export function BankAccountsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(BANK_FEATURE);
  const ledger = useFeatureAccess(ACCOUNT_LEDGER_FEATURE);
  const config = useFeatureConfig(BANK_FEATURE, access.view);
  const modal = useDisclosure<BankAccountRow>();
  const refresh = useRefreshDocuments();
  const types: LookupItem[] = BANK_TYPES.map((id) => ({
    id,
    name: t(`bankAccount.type.${id}`),
    isActive: true,
  }));
  const list = useListState<Filters, StatusView>({
    filters: { type: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = { ...listParams(list.query), type: list.filters.type ?? undefined };
  const query = useListBankAccounts(params, {
    query: {
      queryKey: useUnitKey(getListBankAccountsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const setStatus = useSetBankAccountsStatus();
  const filters = useConfigFilters<keyof Filters>(config.data, { type: types });

  const edit = access.edit ? (row: BankAccountRow) => modal.onOpen(row) : undefined;
  const typeName = (row: BankAccountRow) => t(`bankAccount.type.${row.type}`);
  const { columns, csvColumns } = useMasterColumns<BankAccountRow>(config.data, {
    onOpen: edit,
    renderers: { type: { render: typeName, csv: typeName } },
  });

  return (
    <MasterListPage<BankAccountRow, Filters>
      feature={BANK_FEATURE}
      title={t('bankAccount.title')}
      subtitle={t('bankAccount.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('bankAccount.new')}
      search={{ label: t('bankAccount.search'), placeholder: t('bankAccount.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || !config.data}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('bankAccount.exportFile')}
      fetchAll={() =>
        listBankAccounts({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('bankAccount.emptyTitle'), description: t('bankAccount.emptyText') }}
      onCreate={() => modal.onOpen()}
      onEdit={(row) => modal.onOpen(row)}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      moreActions={(row) => [
        {
          key: 'ledger',
          label: t('bankAccount.ledger'),
          icon: <EyeOutlined />,
          inMenu: true,
          hidden: !ledger.view,
          onClick: () => navigate(accountLedgerLink(row.chartOfAccountId, NO_DATES)),
        },
      ]}
    >
      {config.data && (
        <BankAccountFormModal
          config={config.data}
          open={modal.open}
          bank={modal.data}
          types={types}
          unitName={unit?.name ?? ''}
          onClose={modal.onClose}
          onSaved={refresh}
        />
      )}
    </MasterListPage>
  );
}
