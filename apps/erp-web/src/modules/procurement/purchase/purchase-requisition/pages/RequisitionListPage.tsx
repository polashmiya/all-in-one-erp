import { DownloadOutlined, EditOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons';
import {
  getListPurchaseRequisitionsQueryKey,
  listPurchaseRequisitions,
  problemMessage,
  useListPurchaseRequisitions,
  type RequisitionRow,
} from '@erp/api-client';
import { useConfigColumns } from '@erp/metadata-engine';
import {
  downloadCsv,
  ForbiddenResult,
  formatMoney,
  ListPageLayout,
  TitleCell,
  useListState,
  withDraft,
} from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { App, type TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useConfigFilters } from '@/common/hooks/useConfigFilters';
import { useListDrafts } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { EXPORT_PAGE_SIZE, idParam, listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { RequisitionStatusTag } from '../components/RequisitionStatusTag';
import { useRequisitionActions } from '../components/useRequisitionActions';
import {
  REQUISITION_FEATURE,
  REQUISITION_LOOKUPS,
  REQUISITION_VIEWS,
  requisitionPaths,
  type RequisitionView,
} from '../constants';
import { useRequisitionLookups } from '../requisition-form';

type Filters = {
  warehouseId: string | null;
  requestTypeId: string | null;
  departmentId: string | null;
};

export function RequisitionListPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(REQUISITION_FEATURE);
  const list = useListState<Filters, RequisitionView>({
    filters: { warehouseId: null, requestTypeId: null, departmentId: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams({ ...list.query, view: undefined }),
    status: list.query.view && list.query.view !== 'all' ? list.query.view : undefined,
    warehouseId: list.filters.warehouseId ?? undefined,
    requestTypeId: idParam(list.filters.requestTypeId),
    departmentId: idParam(list.filters.departmentId),
  };
  const query = useListPurchaseRequisitions(params, {
    query: {
      queryKey: useUnitKey(getListPurchaseRequisitionsQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(REQUISITION_FEATURE, access.view);
  const lookups = useRequisitionLookups(access.view);
  const drafts = useListDrafts(
    REQUISITION_FEATURE,
    access.create,
    (draftId) => navigate(withDraft(requisitionPaths.create, draftId)),
    access.view,
  );
  const { actions, modal } = useRequisitionActions();

  const filters = useConfigFilters<keyof Filters>(config.data, {
    warehouseId: lookups[REQUISITION_LOOKUPS.warehouse],
    requestTypeId: lookups[REQUISITION_LOOKUPS.requestType],
    departmentId: lookups[REQUISITION_LOOKUPS.department],
  });

  const { columns: configColumns, csvColumns } = useConfigColumns<RequisitionRow>(config.data, {
    code: {
      render: (row) => (
        <TitleCell
          title={row.code}
          subtitle={t('requisition.itemCount', { count: row.itemCount })}
        />
      ),
    },
  });

  if (!access.view) return <ForbiddenResult />;

  const status = (row: RequisitionRow) => t(`requisition.status.${row.status}`);
  const columns: TableColumnsType<RequisitionRow> = [
    ...configColumns,
    {
      key: 'totalAmount',
      title: t('requisition.col.amount'),
      align: 'end',
      render: (_, row) => formatMoney(row.totalAmount),
    },
    {
      key: 'status',
      title: t('requisition.col.status'),
      render: (_, row) => <RequisitionStatusTag status={row.status} />,
    },
  ];

  const exportRows = async () => {
    try {
      const all = await listPurchaseRequisitions({
        ...params,
        page: 1,
        pageSize: EXPORT_PAGE_SIZE,
      });
      downloadCsv(t('requisition.exportFile'), all.items, [
        ...csvColumns,
        { header: t('requisition.col.items'), value: (r) => r.itemCount },
        { header: t('requisition.col.quantity'), value: (r) => r.totalQuantity },
        { header: t('requisition.col.amount'), value: (r) => r.totalAmount },
        { header: t('requisition.col.status'), value: status },
      ]);
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  const newAction = {
    key: 'new',
    label: t('requisition.new'),
    icon: <PlusOutlined />,
    variant: 'primary' as const,
    hidden: !access.create,
    onClick: () => navigate(requisitionPaths.create),
  };

  return (
    <>
      <ListPageLayout<RequisitionRow, Filters, RequisitionView>
        title={t('requisition.title')}
        subtitle={t('requisition.subtitle', { unit: unit?.name ?? '' })}
        actions={[
          {
            key: 'export',
            label: t('master.export'),
            icon: <DownloadOutlined />,
            hidden: !access.export,
            onClick: exportRows,
          },
          newAction,
        ]}
        state={list}
        search={{ label: t('requisition.searchLabel'), placeholder: t('requisition.search') }}
        filters={filters}
        views={REQUISITION_VIEWS.map((key) => ({
          key,
          label: t(`requisition.view.${key}`),
          count: query.data?.counts[key],
        }))}
        columns={columns}
        rowActions={(row) => [
          {
            key: 'view',
            label: t('master.view'),
            icon: <EyeOutlined />,
            onClick: () => navigate(requisitionPaths.view(row.id)),
          },
          {
            key: 'edit',
            label: t('master.edit'),
            icon: <EditOutlined />,
            hidden: !access.edit || row.status !== 'pending',
            onClick: () => navigate(requisitionPaths.edit(row.id)),
          },
          ...actions(row).map((action) => ({ ...action, inMenu: true })),
        ]}
        rows={query.data?.items}
        rowKey="id"
        total={query.data?.total}
        loading={query.isFetching || config.isPending}
        onRefresh={() => void query.refetch()}
        onRowClick={(row) => navigate(requisitionPaths.view(row.id))}
        empty={{
          title: t('requisition.emptyTitle'),
          description: t('requisition.emptyText'),
          actions: access.create ? [newAction] : undefined,
        }}
        drafts={drafts}
      />
      {modal}
    </>
  );
}
