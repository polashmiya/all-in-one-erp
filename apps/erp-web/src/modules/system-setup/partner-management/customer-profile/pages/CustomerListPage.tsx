import {
  getListCustomersQueryKey,
  listCustomers,
  useListCustomers,
  useSetCustomersStatus,
  type CustomerRow,
} from '@erp/api-client';
import { useConfigColumns } from '@erp/metadata-engine';
import { useListState, withDraft } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  EXPORT_PAGE_SIZE,
  idParam,
  listParams,
  MasterListPage,
  RecordNameCell,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import {
  CUSTOMER_FEATURE,
  customerPaths,
  PARTNER_LOOKUPS,
  useConfigFilters,
  useRefreshPartners,
} from '../../partner-management';
import { useCustomerLookups } from '../customer-form';

type Filters = {
  groupId: string | null;
  priceTypeId: string | null;
  districtId: string | null;
};

export function CustomerListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(CUSTOMER_FEATURE);
  const refresh = useRefreshPartners();
  const list = useListState<Filters, StatusView>({
    filters: { groupId: null, priceTypeId: null, districtId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams(list.query),
    groupId: idParam(list.filters.groupId),
    priceTypeId: idParam(list.filters.priceTypeId),
    districtId: idParam(list.filters.districtId),
  };
  const query = useListCustomers(params, {
    query: {
      queryKey: useUnitKey(getListCustomersQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(CUSTOMER_FEATURE, access.view);
  const lookups = useCustomerLookups(access.view);
  const setStatus = useSetCustomersStatus();

  const filters = useConfigFilters<keyof Filters>(config.data, {
    groupId: lookups[PARTNER_LOOKUPS.customerGroup],
    priceTypeId: lookups[PARTNER_LOOKUPS.priceType],
    districtId: lookups[PARTNER_LOOKUPS.district],
  });

  const { columns, csvColumns } = useConfigColumns<CustomerRow>(config.data, {
    name: {
      render: (row, index) => (
        <RecordNameCell
          name={row.name}
          index={index}
          subtitle={row.billingName}
          onOpen={() => navigate(customerPaths.view(row.id))}
        />
      ),
    },
  });

  return (
    <MasterListPage<CustomerRow, Filters>
      feature={CUSTOMER_FEATURE}
      title={t('customer.title')}
      subtitle={t('customer.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('customer.new')}
      search={{ label: t('customer.searchLabel'), placeholder: t('customer.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      columns={columns}
      csvColumns={csvColumns}
      exportFile={t('customer.exportFile')}
      fetchAll={() =>
        listCustomers({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('customer.emptyTitle'), description: t('customer.emptyText') }}
      onCreate={(draftId) => navigate(withDraft(customerPaths.create, draftId))}
      onEdit={(row) => navigate(customerPaths.edit(row.id))}
      onView={(row) => navigate(customerPaths.view(row.id))}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
    />
  );
}
