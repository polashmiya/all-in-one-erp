import { CalendarOutlined, FileTextOutlined, HomeOutlined, UserOutlined } from '@ant-design/icons';
import {
  getGetPurchaseRequisitionQueryKey,
  useGetPurchaseRequisition,
  type RequisitionLine,
} from '@erp/api-client';
import {
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  formatNumber,
  StatusTag,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { DocumentView } from '../../../components/DocumentView';
import { LinesView } from '../../../components/LinesView';
import { QUANTITY_DECIMALS_SHOWN } from '../../../procurement';
import { REQUISITION_FEATURE, requisitionPaths } from '../constants';
import { requisitionCalls } from '../requisition-form';

export function RequisitionViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(REQUISITION_FEATURE);
  const config = useFeatureConfig(REQUISITION_FEATURE, access.view);
  const query = useGetPurchaseRequisition(id, {
    query: { queryKey: useUnitKey(getGetPurchaseRequisitionQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const requisition = query.data;
  if (!requisition || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('requisition.notFound')}
        missingActions={[
          { key: 'back', label: t('requisition.backToList'), to: requisitionPaths.list },
        ]}
      />
    );
  }

  return (
    <DocumentView
      document={requisition}
      config={config.data}
      feature={REQUISITION_FEATURE}
      paths={requisitionPaths}
      calls={requisitionCalls}
      icon={<FileTextOutlined />}
      tag={requisition.requestTypeName}
      meta={[
        {
          key: 'dates',
          icon: <CalendarOutlined />,
          label: [
            t('requisition.meta.requested', { date: formatDate(requisition.requestDate) }),
            requisition.dueDate
              ? t('requisition.meta.due', { date: formatDate(requisition.dueDate) })
              : t('requisition.meta.noDue'),
          ].join(' · '),
        },
        { key: 'warehouse', icon: <HomeOutlined />, label: requisition.warehouseName },
        {
          key: 'requester',
          icon: <UserOutlined />,
          label: [requisition.employeeName, requisition.departmentName].filter(Boolean).join(' · '),
        },
      ]}
      lines={
        <LinesView<RequisitionLine>
          rows={requisition.lines}
          columns={[
            {
              key: 'ordered',
              title: t('requisition.lines.ordered'),
              align: 'end',
              render: (_, line) => (
                <StatusTag tone={line.orderedQuantity >= line.quantity ? 'success' : 'neutral'}>
                  {formatNumber(line.orderedQuantity, QUANTITY_DECIMALS_SHOWN)}
                </StatusTag>
              ),
            },
            {
              key: 'rate',
              title: t('requisition.lines.rate'),
              align: 'end',
              render: (_, line) => formatMoney(line.rate),
            },
            {
              key: 'amount',
              title: t('requisition.lines.amount'),
              align: 'end',
              render: (_, line) => formatMoney(line.amount),
            },
            {
              key: 'remarks',
              title: t('requisition.lines.remarks'),
              render: (_, line) => line.remarks ?? '—',
            },
          ]}
          totals={[
            {
              key: 'quantity',
              label: t('procurement.lines.totalQuantity'),
              value: formatNumber(requisition.totalQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            {
              key: 'amount',
              label: t('requisition.col.amount'),
              value: formatMoney(requisition.totalAmount),
              strong: true,
            },
          ]}
        />
      }
    />
  );
}
