import { DownloadOutlined, EditOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons';
import { problemMessage, type ApprovalCounts, type FeatureConfig } from '@erp/api-client';
import { useConfigColumns } from '@erp/metadata-engine';
import {
  downloadCsv,
  ForbiddenResult,
  formatMoney,
  ListPageLayout,
  TitleCell,
  withDraft,
  type ListFilterDef,
  type ListFilters,
  type ListState,
} from '@erp/ui';
import { App, type TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useListDrafts } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { APPROVAL_VIEWS, type ApprovalView, type DocumentPaths } from '../procurement';
import { ApprovalStatusTag } from './ApprovalStatusTag';
import { useApprovalActions, type ApprovalCalls } from './useApprovalActions';

interface DocumentRow {
  id: number;
  code: string;
  status: string;
  itemCount?: number;
  totalQuantity?: number;
}

export interface DocumentListPageProps<T extends DocumentRow, F extends ListFilters> {
  feature: string;
  config?: FeatureConfig;
  title: string;
  subtitle: string;
  newLabel: string;
  search: { label: string; placeholder: string };
  list: ListState<F, ApprovalView>;
  filters: ListFilterDef<F>[];
  data?: { items: T[]; total: number; counts: ApprovalCounts };
  loading: boolean;
  onRefresh: () => void;
  amount: { label: string; value: (row: T) => number };
  lines?: { label: string; count: (row: T) => number; subtitle: (count: number) => string };
  exportFile: string;
  fetchAll: () => Promise<T[]>;
  empty: { title: string; description: string };
  paths: DocumentPaths;
  calls: ApprovalCalls;
}

export function DocumentListPage<T extends DocumentRow, F extends ListFilters>({
  feature,
  config,
  title,
  subtitle,
  newLabel,
  search,
  list,
  filters,
  data,
  loading,
  onRefresh,
  amount,
  lines,
  exportFile,
  fetchAll,
  empty,
  paths,
  calls,
}: DocumentListPageProps<T, F>) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const access = useFeatureAccess(feature);
  const drafts = useListDrafts(
    feature,
    access.create,
    (draftId) => navigate(withDraft(paths.create, draftId)),
    access.view,
  );
  const { actions, modal } = useApprovalActions(feature, calls);
  const lineInfo = lines ?? {
    label: t('procurement.col.items'),
    count: (row: T) => row.itemCount ?? 0,
    subtitle: (count: number) => t('procurement.itemCount', { count }),
  };
  const { columns: configColumns, csvColumns } = useConfigColumns<T>(config, {
    code: {
      render: (row) => (
        <TitleCell title={row.code} subtitle={lineInfo.subtitle(lineInfo.count(row))} />
      ),
    },
  });

  if (!access.view) return <ForbiddenResult />;

  const status = (row: T) => t(`procurement.status.${row.status}`);
  const columns: TableColumnsType<T> = [
    ...configColumns,
    {
      key: 'amount',
      title: amount.label,
      align: 'end',
      render: (_, row) => formatMoney(amount.value(row)),
    },
    {
      key: 'status',
      title: t('procurement.col.status'),
      render: (_, row) => <ApprovalStatusTag status={row.status} />,
    },
  ];

  const exportRows = async () => {
    try {
      downloadCsv(exportFile, await fetchAll(), [
        ...csvColumns,
        { header: lineInfo.label, value: lineInfo.count },
        ...(lines
          ? []
          : [{ header: t('procurement.col.quantity'), value: (r: T) => r.totalQuantity }]),
        { header: amount.label, value: amount.value },
        { header: t('procurement.col.status'), value: status },
      ]);
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  const newAction = {
    key: 'new',
    label: newLabel,
    icon: <PlusOutlined />,
    variant: 'primary' as const,
    hidden: !access.create,
    onClick: () => navigate(paths.create),
  };

  return (
    <>
      <ListPageLayout<T, F, ApprovalView>
        title={title}
        subtitle={subtitle}
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
        search={search}
        filters={filters}
        views={APPROVAL_VIEWS.map((key) => ({
          key,
          label: t(`procurement.view.${key}`),
          count: data?.counts[key],
        }))}
        columns={columns}
        rowActions={(row) => [
          {
            key: 'view',
            label: t('master.view'),
            icon: <EyeOutlined />,
            onClick: () => navigate(paths.view(row.id)),
          },
          {
            key: 'edit',
            label: t('master.edit'),
            icon: <EditOutlined />,
            hidden: !access.edit || row.status !== 'pending',
            onClick: () => navigate(paths.edit(row.id)),
          },
          ...actions(row).map((action) => ({ ...action, inMenu: true })),
        ]}
        rows={data?.items}
        rowKey={(row) => row.id}
        total={data?.total}
        loading={loading}
        onRefresh={onRefresh}
        onRowClick={(row) => navigate(paths.view(row.id))}
        empty={{ ...empty, actions: access.create ? [newAction] : undefined }}
        drafts={drafts}
      />
      {modal}
    </>
  );
}
