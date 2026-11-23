import {
  CheckCircleOutlined,
  ClockCircleOutlined,
  CloseCircleOutlined,
  EyeOutlined,
  FileDoneOutlined,
} from '@ant-design/icons';
import {
  getGetApprovalInboxQueryKey,
  problemMessage,
  useGetApprovalInbox,
  type ApprovalInboxRow,
} from '@erp/api-client';
import {
  ForbiddenResult,
  formatDate,
  formatDateTime,
  formatMoney,
  ListPageLayout,
  StatGrid,
  TitleCell,
  useListState,
} from '@erp/ui';
import { keepPreviousData, useQueryClient } from '@tanstack/react-query';
import { App, type TableColumnsType } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { ReviewModal, type ReviewMode } from '@/common/approval/ReviewModal';
import { appConfig } from '@/common/config/app.config';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { listParams, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { PROCUREMENT_API } from '@/modules/procurement/procurement';
import {
  APPROVAL_DOCUMENTS,
  INBOX_VIEWS,
  MY_APPROVALS_FEATURE,
  type InboxView,
} from '../constants';

type Filters = { feature: string | null };

const APPROVALS_API = '/api/erp/approvals';

export function MyApprovalsPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const client = useQueryClient();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(MY_APPROVALS_FEATURE);
  const [review, setReview] = useState<{ row: ApprovalInboxRow; mode: ReviewMode }>();
  const list = useListState<Filters, InboxView>({
    filters: { feature: null },
    view: 'waiting',
    pageSize: appConfig.defaultPageSize,
  });

  const { search, page, pageSize } = listParams({ ...list.query, view: undefined });
  const params = {
    view: list.query.view,
    feature: list.filters.feature ?? undefined,
    search,
    page,
    pageSize,
  };
  const query = useGetApprovalInbox(params, {
    query: {
      queryKey: useUnitKey(getGetApprovalInboxQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });

  if (!access.view) return <ForbiddenResult />;

  const waiting = list.query.view === 'waiting';
  const counts = query.data?.counts;
  const documentOf = (row: ApprovalInboxRow) => APPROVAL_DOCUMENTS[row.feature];
  const open = (row: ApprovalInboxRow) => {
    const document = documentOf(row);
    if (document) void navigate(document.path(row.id));
  };

  const decide = async (comment: string | null) => {
    if (!review) return;
    const { row, mode } = review;
    const document = documentOf(row);
    if (!document) return;
    try {
      await (mode === 'approve' ? document.approve : document.reject)(row.id, { comment });
      void message.success(
        t(mode === 'approve' ? 'myApprovals.done.approved' : 'myApprovals.done.rejected', {
          code: row.code,
        }),
      );
      await client.invalidateQueries({
        predicate: ({ queryKey }) =>
          typeof queryKey[0] === 'string' &&
          (queryKey[0].startsWith(APPROVALS_API) || queryKey[0].startsWith(PROCUREMENT_API)),
      });
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
      throw error;
    }
  };

  const columns: TableColumnsType<ApprovalInboxRow> = [
    {
      key: 'document',
      title: t('myApprovals.col.document'),
      render: (_, row) => (
        <TitleCell
          leading={<FileDoneOutlined className="erp-fg--primary" />}
          title={row.code}
          subtitle={documentOf(row) ? t(documentOf(row)!.label) : row.feature}
        />
      ),
    },
    { key: 'date', title: t('myApprovals.col.date'), render: (_, row) => formatDate(row.date) },
    { key: 'party', title: t('myApprovals.col.party'), render: (_, row) => row.party ?? '—' },
    {
      key: 'amount',
      title: t('myApprovals.col.amount'),
      align: 'end',
      render: (_, row) => formatMoney(row.amount),
    },
    {
      key: 'step',
      title: t('myApprovals.col.step'),
      render: (_, row) => (
        <TitleCell
          title={row.stepName}
          subtitle={t('myApprovals.step', { level: row.level, levels: row.levels })}
        />
      ),
    },
    {
      key: 'requestedBy',
      title: t('myApprovals.col.requestedBy'),
      render: (_, row) => row.requestedBy ?? '—',
    },
    waiting
      ? {
          key: 'submitted',
          title: t('myApprovals.col.submitted'),
          render: (_, row) => formatDateTime(row.submittedAt),
        }
      : {
          key: 'decided',
          title: t('myApprovals.col.decided'),
          render: (_, row) => formatDateTime(row.actedAt),
        },
    ...(waiting
      ? []
      : [
          {
            key: 'comment',
            title: t('myApprovals.col.comment'),
            render: (_: unknown, row: ApprovalInboxRow) => row.comment ?? '—',
          },
        ]),
  ];

  return (
    <>
      <ListPageLayout<ApprovalInboxRow, Filters, InboxView>
        title={t('myApprovals.title')}
        subtitle={t('myApprovals.subtitle', { unit: unit?.name ?? '' })}
        summary={
          <StatGrid
            items={[
              {
                key: 'waiting',
                icon: <ClockCircleOutlined />,
                tint: 'amber',
                label: t('myApprovals.stat.waiting'),
                value: counts?.waiting ?? 0,
                caption: t('myApprovals.stat.waitingHint'),
              },
              {
                key: 'approved',
                icon: <CheckCircleOutlined />,
                tint: 'green',
                label: t('myApprovals.stat.approved'),
                value: counts?.approved ?? 0,
              },
              {
                key: 'rejected',
                icon: <CloseCircleOutlined />,
                tint: 'slate',
                label: t('myApprovals.stat.rejected'),
                value: counts?.rejected ?? 0,
              },
            ]}
          />
        }
        state={list}
        search={{ label: t('myApprovals.search'), placeholder: t('myApprovals.search') }}
        filters={[
          {
            key: 'feature',
            label: t('myApprovals.filter.document'),
            options: Object.entries(APPROVAL_DOCUMENTS).map(([value, document]) => ({
              value,
              label: t(document.label),
            })),
          },
        ]}
        views={INBOX_VIEWS.map((key) => ({
          key,
          label: t(`myApprovals.view.${key}`),
          count: counts?.[key],
        }))}
        columns={columns}
        rowActions={(row) => [
          {
            key: 'open',
            label: t('myApprovals.open'),
            icon: <EyeOutlined />,
            onClick: () => open(row),
          },
          {
            key: 'approve',
            label: t('approval.approve'),
            icon: <CheckCircleOutlined />,
            hidden: !waiting,
            onClick: () => setReview({ row, mode: 'approve' }),
          },
          {
            key: 'reject',
            label: t('approval.reject'),
            icon: <CloseCircleOutlined />,
            inMenu: true,
            danger: true,
            hidden: !waiting,
            onClick: () => setReview({ row, mode: 'reject' }),
          },
        ]}
        rows={query.data?.items}
        rowKey={(row) => `${row.feature}:${row.id}:${row.level}`}
        total={query.data?.total}
        loading={query.isFetching}
        onRefresh={() => void query.refetch()}
        onRowClick={open}
        empty={
          waiting
            ? {
                title: t('myApprovals.emptyWaiting'),
                description: t('myApprovals.emptyWaitingText'),
              }
            : { title: t('myApprovals.emptyHistory') }
        }
      />
      <ReviewModal
        mode={review?.mode}
        code={review?.row.code ?? ''}
        onClose={() => setReview(undefined)}
        onSubmit={decide}
      />
    </>
  );
}
