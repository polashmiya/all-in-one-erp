import {
  CalendarOutlined,
  FileDoneOutlined,
  HomeOutlined,
  RollbackOutlined,
  ShopOutlined,
} from '@ant-design/icons';
import {
  getGetPurchaseReturnQueryKey,
  useGetPurchaseReturn,
  type PurchaseReturnLineDetail,
} from '@erp/api-client';
import {
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  formatNumber,
  formatPercent,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { DocumentView } from '@/common/documents/DocumentView';
import { LinesView } from '@/common/documents/LinesView';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { receivePaths } from '../../purchase-receive/constants';
import { RETURN_FEATURE, returnPaths } from '../constants';
import { returnCalls } from '../return-form';

export function ReturnViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(RETURN_FEATURE);
  const config = useFeatureConfig(RETURN_FEATURE, access.view);
  const query = useGetPurchaseReturn(id, {
    query: { queryKey: useUnitKey(getGetPurchaseReturnQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const purchaseReturn = query.data;
  if (!purchaseReturn || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('return.notFound')}
        missingActions={[{ key: 'back', label: t('return.backToList'), to: returnPaths.list }]}
      />
    );
  }

  return (
    <DocumentView
      document={purchaseReturn}
      config={config.data}
      feature={RETURN_FEATURE}
      paths={returnPaths}
      calls={returnCalls}
      icon={<RollbackOutlined />}
      tag={purchaseReturn.reasonName}
      meta={[
        { key: 'supplier', icon: <ShopOutlined />, label: purchaseReturn.supplierName },
        {
          key: 'receive',
          icon: <FileDoneOutlined />,
          label: (
            <Link to={receivePaths.view(purchaseReturn.purchaseReceiveId)}>
              {[purchaseReturn.purchaseReceiveName, purchaseReturn.purchaseOrderName]
                .filter(Boolean)
                .join(' · ')}
            </Link>
          ),
        },
        {
          key: 'date',
          icon: <CalendarOutlined />,
          label: t('return.meta.returned', { date: formatDate(purchaseReturn.returnDate) }),
        },
        { key: 'warehouse', icon: <HomeOutlined />, label: purchaseReturn.warehouseName },
      ]}
      lines={
        <LinesView<PurchaseReturnLineDetail>
          rows={purchaseReturn.lines}
          columns={[
            {
              key: 'price',
              title: t('document.lines.price'),
              align: 'end',
              render: (_, line) => formatMoney(line.price),
            },
            {
              key: 'discount',
              title: t('document.lines.discount'),
              align: 'end',
              render: (_, line) => formatMoney(line.discount),
            },
            {
              key: 'vat',
              title: t('document.lines.vat'),
              align: 'end',
              render: (_, line) =>
                `${formatPercent(line.vatPercentage)} · ${formatMoney(line.vatAmount)}`,
            },
            {
              key: 'net',
              title: t('document.lines.net'),
              align: 'end',
              render: (_, line) => formatMoney(line.netAmount),
            },
            {
              key: 'batch',
              title: t('return.lines.batch'),
              render: (_, line) => line.batch ?? '—',
            },
          ]}
          totals={[
            {
              key: 'quantity',
              label: t('document.lines.totalQuantity'),
              value: formatNumber(purchaseReturn.totalQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            {
              key: 'vat',
              label: t('document.totals.vat'),
              value: formatMoney(purchaseReturn.totalVatAmount),
            },
            {
              key: 'net',
              label: t('document.totals.net'),
              value: formatMoney(purchaseReturn.netAmount),
              strong: true,
            },
          ]}
        />
      }
    />
  );
}
