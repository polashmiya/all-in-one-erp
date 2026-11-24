import { CalendarOutlined, HomeOutlined, SwapOutlined } from '@ant-design/icons';
import {
  getGetStockTransferQueryKey,
  useGetStockTransfer,
  type StockTransferLineDetail,
} from '@erp/api-client';
import { DetailsPageLayout, ForbiddenResult, formatDate, formatNumber } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { DocumentView } from '@/common/documents/DocumentView';
import { LinesView } from '@/common/documents/LinesView';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { TRANSFER_FEATURE, transferPaths } from '../constants';
import { transferCalls } from '../transfer-form';

const place = (office: string | null, warehouse: string | null) =>
  [office, warehouse].filter(Boolean).join(' · ');

export function TransferViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(TRANSFER_FEATURE);
  const config = useFeatureConfig(TRANSFER_FEATURE, access.view);
  const query = useGetStockTransfer(id, {
    query: { queryKey: useUnitKey(getGetStockTransferQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const transfer = query.data;
  if (!transfer || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('transfer.notFound')}
        missingActions={[{ key: 'back', label: t('transfer.backToList'), to: transferPaths.list }]}
      />
    );
  }

  return (
    <DocumentView
      document={transfer}
      config={config.data}
      feature={TRANSFER_FEATURE}
      paths={transferPaths}
      calls={transferCalls}
      icon={<SwapOutlined />}
      meta={[
        { key: 'date', icon: <CalendarOutlined />, label: formatDate(transfer.transferDate) },
        {
          key: 'warehouses',
          icon: <HomeOutlined />,
          label: `${place(transfer.fromOfficeName, transfer.fromWarehouseName)} → ${place(transfer.toOfficeName, transfer.toWarehouseName)}`,
        },
      ]}
      lines={
        <LinesView<StockTransferLineDetail>
          rows={transfer.lines}
          columns={[
            {
              key: 'remarks',
              title: t('transfer.lines.remarks'),
              render: (_, line) => line.remarks ?? '—',
            },
          ]}
          totals={[
            {
              key: 'quantity',
              label: t('document.lines.totalQuantity'),
              value: formatNumber(transfer.totalQuantity, QUANTITY_DECIMALS_SHOWN),
              strong: true,
            },
          ]}
        />
      }
    />
  );
}
