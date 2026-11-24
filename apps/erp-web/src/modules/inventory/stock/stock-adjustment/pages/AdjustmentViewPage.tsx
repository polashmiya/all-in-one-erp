import { CalendarOutlined, ControlOutlined, HomeOutlined } from '@ant-design/icons';
import {
  getGetStockAdjustmentQueryKey,
  useGetStockAdjustment,
  type StockAdjustmentLineDetail,
} from '@erp/api-client';
import { DetailsPageLayout, ForbiddenResult, formatDate, formatMoney, formatNumber } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { DocumentView } from '@/common/documents/DocumentView';
import { LinesView } from '@/common/documents/LinesView';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { adjustmentCalls } from '../adjustment-form';
import { ADJUSTMENT_FEATURE, adjustmentPaths } from '../constants';

export function AdjustmentViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(ADJUSTMENT_FEATURE);
  const config = useFeatureConfig(ADJUSTMENT_FEATURE, access.view);
  const query = useGetStockAdjustment(id, {
    query: { queryKey: useUnitKey(getGetStockAdjustmentQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const adjustment = query.data;
  if (!adjustment || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('adjustment.notFound')}
        missingActions={[
          { key: 'back', label: t('adjustment.backToList'), to: adjustmentPaths.list },
        ]}
      />
    );
  }

  const typeName = t(`adjustment.type.${adjustment.type}`);

  return (
    <DocumentView
      document={adjustment}
      config={config.data}
      feature={ADJUSTMENT_FEATURE}
      paths={adjustmentPaths}
      calls={adjustmentCalls}
      icon={<ControlOutlined />}
      tag={typeName}
      renderers={{ type: () => typeName }}
      meta={[
        {
          key: 'date',
          icon: <CalendarOutlined />,
          label: formatDate(adjustment.adjustmentDate),
        },
        { key: 'warehouse', icon: <HomeOutlined />, label: adjustment.warehouseName },
      ]}
      lines={
        <LinesView<StockAdjustmentLineDetail>
          rows={adjustment.lines}
          columns={[
            {
              key: 'rate',
              title: t('adjustment.lines.rate'),
              align: 'end',
              render: (_, line) => formatMoney(line.rate),
            },
            {
              key: 'amount',
              title: t('adjustment.lines.amount'),
              align: 'end',
              render: (_, line) => formatMoney(line.amount),
            },
            {
              key: 'remarks',
              title: t('adjustment.lines.remarks'),
              render: (_, line) => line.remarks ?? '—',
            },
          ]}
          totals={[
            {
              key: 'quantity',
              label: t('document.lines.totalQuantity'),
              value: formatNumber(adjustment.totalQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            {
              key: 'amount',
              label: t('adjustment.totals.amount'),
              value: formatMoney(adjustment.totalAmount),
              strong: true,
            },
          ]}
        />
      }
    />
  );
}
