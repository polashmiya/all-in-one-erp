import {
  getListTaxDepositsQueryKey,
  listTaxDeposits,
  useListTaxDeposits,
  type TaxDepositRow,
} from '@erp/api-client';
import { useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { DocumentListPage } from '@/common/documents/DocumentListPage';
import { statusParam, type ApprovalView } from '@/common/documents/documents';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { DEPOSIT_FEATURE, depositPaths } from '../constants';
import { depositCalls, useTaxTypes } from '../deposit-form';

type Filters = { type: string | null };

export function DepositListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(DEPOSIT_FEATURE);
  const types = useTaxTypes();
  const list = useListState<Filters, ApprovalView>({
    filters: { type: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    type: list.filters.type ?? undefined,
  };
  const query = useListTaxDeposits(params, {
    query: {
      queryKey: useUnitKey(getListTaxDepositsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(DEPOSIT_FEATURE, access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, { type: types });
  const typeName = (row: TaxDepositRow) => t(`deposit.type.${row.type}`);

  return (
    <DocumentListPage<TaxDepositRow, Filters>
      feature={DEPOSIT_FEATURE}
      config={config.data}
      title={t('deposit.title')}
      subtitle={t('deposit.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('deposit.new')}
      search={{ label: t('deposit.searchLabel'), placeholder: t('deposit.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('deposit.total'), value: (row) => row.totalAmount }}
      lines={{
        label: t('deposit.col.payments'),
        value: (row) => row.paymentCount,
        subtitle: (row) => t('deposit.paymentCount', { count: row.paymentCount }),
      }}
      renderers={{ type: { render: typeName, csv: typeName } }}
      exportFile={t('deposit.exportFile')}
      fetchAll={() =>
        listTaxDeposits({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('deposit.emptyTitle'), description: t('deposit.emptyText') }}
      paths={depositPaths}
      calls={depositCalls}
    />
  );
}
