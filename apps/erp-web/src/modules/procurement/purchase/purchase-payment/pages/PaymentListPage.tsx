import {
  getListPurchasePaymentsQueryKey,
  listPurchasePayments,
  useListPurchasePayments,
  type PurchasePaymentRow,
} from '@erp/api-client';
import { useListState } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { DocumentListPage } from '@/common/documents/DocumentListPage';
import { statusParam, type ApprovalView } from '@/common/documents/documents';
import { PAYMENT_FEATURE, PAYMENT_LOOKUPS, paymentPaths } from '../constants';
import { paymentCalls, usePaymentLookups } from '../payment-form';

type Filters = {
  supplierId: string | null;
  paymentMethodId: string | null;
};

export function PaymentListPage() {
  const { t } = useTranslation();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(PAYMENT_FEATURE);
  const list = useListState<Filters, ApprovalView>({
    filters: { supplierId: null, paymentMethodId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: statusParam(list.query.view),
    supplierId: idParam(list.filters.supplierId),
    paymentMethodId: idParam(list.filters.paymentMethodId),
  };
  const query = useListPurchasePayments(params, {
    query: {
      queryKey: useUnitKey(getListPurchasePaymentsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(PAYMENT_FEATURE, access.view);
  const { lookups } = usePaymentLookups(access.view);
  const filters = useConfigFilters<keyof Filters>(config.data, {
    supplierId: lookups[PAYMENT_LOOKUPS.supplier],
    paymentMethodId: lookups[PAYMENT_LOOKUPS.method],
  });

  return (
    <DocumentListPage<PurchasePaymentRow, Filters>
      feature={PAYMENT_FEATURE}
      config={config.data}
      title={t('payment.title')}
      subtitle={t('payment.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('payment.new')}
      search={{ label: t('payment.searchLabel'), placeholder: t('payment.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      amount={{ label: t('payment.totals.payment'), value: (row) => row.paymentAmount }}
      lines={{
        label: t('payment.col.bills'),
        value: (row) => row.billCount,
        subtitle: (row) =>
          row.isAdvance ? t('payment.advance') : t('payment.billCount', { count: row.billCount }),
      }}
      exportFile={t('payment.exportFile')}
      fetchAll={() =>
        listPurchasePayments({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then(
          (r) => r.items,
        )
      }
      empty={{ title: t('payment.emptyTitle'), description: t('payment.emptyText') }}
      paths={paymentPaths}
      calls={paymentCalls}
    />
  );
}
