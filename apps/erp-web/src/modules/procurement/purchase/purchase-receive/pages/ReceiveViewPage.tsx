import {
  CalendarOutlined,
  FileDoneOutlined,
  HomeOutlined,
  InboxOutlined,
  ShopOutlined,
} from '@ant-design/icons';
import {
  getGetPurchaseReceiveQueryKey,
  useGetPurchaseReceive,
  type PurchaseReceiveLineDetail,
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
import { DocumentView } from '../../../components/DocumentView';
import { LinesView } from '../../../components/LinesView';
import { QUANTITY_DECIMALS_SHOWN } from '../../../procurement';
import { orderPaths } from '../../purchase-order/constants';
import { RECEIVE_FEATURE, receivePaths } from '../constants';
import { receiveCalls } from '../receive-form';

export function ReceiveViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(RECEIVE_FEATURE);
  const config = useFeatureConfig(RECEIVE_FEATURE, access.view);
  const query = useGetPurchaseReceive(id, {
    query: { queryKey: useUnitKey(getGetPurchaseReceiveQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const receive = query.data;
  if (!receive || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('receive.notFound')}
        missingActions={[{ key: 'back', label: t('receive.backToList'), to: receivePaths.list }]}
      />
    );
  }

  const charges = receive.deliveryCharge + receive.laborCharge + receive.otherCharge;

  return (
    <DocumentView
      document={receive}
      config={config.data}
      feature={RECEIVE_FEATURE}
      paths={receivePaths}
      calls={receiveCalls}
      icon={<InboxOutlined />}
      tag={receive.paymentTypeName}
      meta={[
        { key: 'supplier', icon: <ShopOutlined />, label: receive.supplierName },
        {
          key: 'order',
          icon: <FileDoneOutlined />,
          label: (
            <Link to={orderPaths.view(receive.purchaseOrderId)}>{receive.purchaseOrderName}</Link>
          ),
        },
        {
          key: 'date',
          icon: <CalendarOutlined />,
          label: t('receive.meta.received', { date: formatDate(receive.receiveDate) }),
        },
        { key: 'warehouse', icon: <HomeOutlined />, label: receive.warehouseName },
      ]}
      lines={
        <LinesView<PurchaseReceiveLineDetail>
          rows={receive.lines}
          columns={[
            {
              key: 'price',
              title: t('procurement.lines.price'),
              align: 'end',
              render: (_, line) => formatMoney(line.price),
            },
            {
              key: 'discount',
              title: t('procurement.lines.discount'),
              align: 'end',
              render: (_, line) => formatMoney(line.discount),
            },
            {
              key: 'vat',
              title: t('procurement.lines.vat'),
              align: 'end',
              render: (_, line) =>
                `${formatPercent(line.vatPercentage)} · ${formatMoney(line.vatAmount)}`,
            },
            {
              key: 'net',
              title: t('procurement.lines.net'),
              align: 'end',
              render: (_, line) => formatMoney(line.netAmount),
            },
            {
              key: 'returned',
              title: t('receive.lines.returned'),
              align: 'end',
              render: (_, line) => formatNumber(line.returnedQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            {
              key: 'batch',
              title: t('receive.lines.batch'),
              render: (_, line) => line.batch ?? '—',
            },
          ]}
          totals={[
            {
              key: 'quantity',
              label: t('procurement.lines.totalQuantity'),
              value: formatNumber(receive.totalQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            {
              key: 'vat',
              label: t('procurement.totals.vat'),
              value: formatMoney(receive.totalVatAmount),
            },
            {
              key: 'items',
              label: t('procurement.totals.items'),
              value: formatMoney(receive.itemTotalAmount),
            },
            {
              key: 'discount',
              label: t('procurement.totals.discount'),
              value: formatMoney(receive.headerDiscount),
            },
            { key: 'charges', label: t('procurement.totals.charges'), value: formatMoney(charges) },
            {
              key: 'net',
              label: t('procurement.totals.net'),
              value: formatMoney(receive.netAmount),
              strong: true,
            },
            ...(receive.returnAmount
              ? [
                  {
                    key: 'returned',
                    label: t('receive.totals.returned'),
                    value: formatMoney(receive.returnAmount),
                  },
                ]
              : []),
          ]}
        />
      }
    />
  );
}
