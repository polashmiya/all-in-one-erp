import {
  CalendarOutlined,
  CheckCircleOutlined,
  LineChartOutlined,
  LockOutlined,
  SettingOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import {
  closePeriod,
  getGetPeriodClosingsQueryKey,
  problemMessage,
  reopenPeriod,
  useGetPeriodClosings,
  type ClosingMonth,
  type ClosingYear,
} from '@erp/api-client';
import {
  ForbiddenResult,
  formatDate,
  formatDateTime,
  formatMoney,
  ListPageLayout,
  StatGrid,
  StatusTag,
  useDisclosure,
  useListState,
  type ActionItem,
} from '@erp/ui';
import { App, type TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { useRefreshDocuments } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { EXPORT_PAGE_SIZE, useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { FiscalYearModal } from '../components/FiscalYearModal';
import { PERIOD_CLOSING_FEATURE } from '../constants';

type Filters = { year: string | null };
type Change = 'close' | 'reopen';

const monthName = (month: ClosingMonth) => formatDate(month.start, 'MMMM YYYY');
const yearName = (year: ClosingYear) =>
  `${formatDate(year.start, 'MMM YYYY')} – ${formatDate(year.end, 'MMM YYYY')}`;

export function PeriodClosingPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(PERIOD_CLOSING_FEATURE);
  const refresh = useRefreshDocuments();
  const fiscalYear = useDisclosure();
  const list = useListState<Filters>({ filters: { year: null }, pageSize: EXPORT_PAGE_SIZE });
  const query = useGetPeriodClosings({
    query: { queryKey: useUnitKey(getGetPeriodClosingsQueryKey()), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const data = query.data;
  const year = data?.years.find((y) => y.end === list.filters.year) ?? data?.years[0];

  const run = async (change: Change, periodEnd: string, isYear: boolean, name: string) => {
    try {
      await (change === 'close' ? closePeriod : reopenPeriod)({ periodEnd, isYear });
      void message.success(t(`period.done.${change}`, { name }));
      await refresh();
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  const yearAction = (change: Change, of: ClosingYear): ActionItem => {
    const label = t(`period.${change}Year`);
    return {
      key: `${change}Year`,
      label,
      icon: change === 'close' ? <LockOutlined /> : <UnlockOutlined />,
      confirm: {
        title: t(`period.${change}Confirm`, { name: yearName(of) }),
        description: t(`period.${change}YearHint`),
        okText: label,
      },
      onClick: () => run(change, of.end, true, yearName(of)),
    };
  };

  const columns: TableColumnsType<ClosingMonth> = [
    { key: 'month', title: t('period.col.month'), render: (_, row) => monthName(row) },
    {
      key: 'status',
      title: t('period.col.status'),
      render: (_, row) => (
        <StatusTag tone={row.isClosed ? 'neutral' : 'success'} dot>
          {t(row.isClosed ? 'period.closed' : 'period.open')}
        </StatusTag>
      ),
    },
    {
      key: 'pending',
      title: t('period.col.pending'),
      align: 'end',
      render: (_, row) => row.pending || '—',
    },
    {
      key: 'closedBy',
      title: t('period.col.closedBy'),
      render: (_, row) => row.closedByName ?? '—',
    },
    {
      key: 'closedAt',
      title: t('period.col.closedAt'),
      render: (_, row) => (row.closedAt ? formatDateTime(row.closedAt) : '—'),
    },
  ];

  const actions: ActionItem[] = [
    {
      key: 'fiscalYear',
      label: t('period.fiscalYear'),
      icon: <SettingOutlined />,
      hidden: !access.edit || !data?.canChangeStart,
      onClick: () => fiscalYear.onOpen(),
    },
    ...(year
      ? [
          { ...yearAction('reopen', year), hidden: !access.edit || !year.canReopen },
          {
            ...yearAction('close', year),
            variant: 'primary' as const,
            hidden: !access.edit || year.isClosed,
            disabled: !year.canClose,
          },
        ]
      : []),
  ];

  return (
    <>
      <ListPageLayout<ClosingMonth, Filters, string>
        title={t('period.title')}
        subtitle={t('period.subtitle', { unit: unit?.name ?? '' })}
        actions={actions}
        summary={
          <StatGrid
            items={[
              {
                key: 'through',
                icon: <CalendarOutlined />,
                tint: 'blue',
                label: t('period.stat.closedThrough'),
                value: data?.closedThrough ? formatDate(data.closedThrough) : t('period.stat.none'),
              },
              {
                key: 'year',
                icon: <CheckCircleOutlined />,
                tint: year?.isClosed ? 'slate' : 'green',
                label: t('period.stat.year'),
                value: t(year?.isClosed ? 'period.closed' : 'period.open'),
                caption: year && yearName(year),
              },
              {
                key: 'profit',
                icon: <LineChartOutlined />,
                tint: 'green',
                label: t('period.stat.profit'),
                value: formatMoney(year?.profit ?? 0),
              },
            ]}
          />
        }
        state={list}
        filters={[
          {
            key: 'year',
            label: t('period.filter.year'),
            options: (data?.years ?? []).map((y) => ({ value: y.end, label: yearName(y) })),
          },
        ]}
        columns={columns}
        rowActions={(row) =>
          (['close', 'reopen'] as const).map((change) => ({
            key: change,
            label: t(`period.${change}`),
            icon: change === 'close' ? <LockOutlined /> : <UnlockOutlined />,
            hidden: !access.edit || !(change === 'close' ? row.canClose : row.canReopen),
            confirm: {
              title: t(`period.${change}Confirm`, { name: monthName(row) }),
              description: t(`period.${change}Hint`),
              okText: t(`period.${change}`),
            },
            onClick: () => run(change, row.end, false, monthName(row)),
          }))
        }
        rows={year?.months}
        rowKey="end"
        total={year?.months.length}
        loading={query.isFetching}
        empty={{ title: t('report.emptyTitle') }}
      />
      {data && (
        <FiscalYearModal
          open={fiscalYear.open}
          startMonth={data.fiscalYearStartMonth}
          onClose={fiscalYear.onClose}
        />
      )}
    </>
  );
}
