import { CalendarOutlined, HomeOutlined, ShopOutlined, ShoppingOutlined } from '@ant-design/icons';
import {
  getGetPurchaseOrderQueryKey,
  useGetPurchaseOrder,
  type PurchaseOrderLineDetail,
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
import { useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { DocumentView } from '../../../components/DocumentView';
import { LinesView } from '../../../components/LinesView';
import { QUANTITY_DECIMALS_SHOWN } from '../../../procurement';
import { ORDER_FEATURE, orderPaths } from '../constants';
import { orderCalls } from '../order-form';

export function OrderViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(ORDER_FEATURE);
  const config = useFeatureConfig(ORDER_FEATURE, access.view);
  const query = useGetPurchaseOrder(id, {
    query: { queryKey: useUnitKey(getGetPurchaseOrderQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const order = query.data;
  if (!order || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('order.notFound')}
        missingActions={[{ key: 'back', label: t('order.backToList'), to: orderPaths.list }]}
      />
    );
  }

  const charges = order.deliveryCharge + order.laborCharge + order.otherCharge;

  return (
    <DocumentView
      document={order}
      config={config.data}
      feature={ORDER_FEATURE}
      paths={orderPaths}
      calls={orderCalls}
      icon={<ShoppingOutlined />}
      tag={order.paymentTypeName}
      meta={[
        { key: 'supplier', icon: <ShopOutlined />, label: order.supplierName },
        {
          key: 'dates',
          icon: <CalendarOutlined />,
          label: [
            t('order.meta.ordered', { date: formatDate(order.orderDate) }),
            order.expectedReceiveDate &&
              t('order.meta.expected', { date: formatDate(order.expectedReceiveDate) }),
          ]
            .filter(Boolean)
            .join(' · '),
        },
        { key: 'warehouse', icon: <HomeOutlined />, label: order.warehouseName },
      ]}
      lines={
        <LinesView<PurchaseOrderLineDetail>
          rows={order.lines}
          columns={[
            {
              key: 'price',
              title: t('order.lines.price'),
              align: 'end',
              render: (_, line) => formatMoney(line.price),
            },
            {
              key: 'discount',
              title: t('order.lines.discount'),
              align: 'end',
              render: (_, line) => formatMoney(line.discount),
            },
            {
              key: 'vat',
              title: t('order.lines.vat'),
              align: 'end',
              render: (_, line) =>
                `${formatPercent(line.vatPercentage)} · ${formatMoney(line.vatAmount)}`,
            },
            {
              key: 'net',
              title: t('order.lines.net'),
              align: 'end',
              render: (_, line) => formatMoney(line.netAmount),
            },
            {
              key: 'received',
              title: t('order.lines.received'),
              align: 'end',
              render: (_, line) => formatNumber(line.receivedQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            {
              key: 'specification',
              title: t('order.lines.specification'),
              render: (_, line) => line.specification ?? '—',
            },
          ]}
          totals={[
            {
              key: 'quantity',
              label: t('procurement.lines.totalQuantity'),
              value: formatNumber(order.totalQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            { key: 'vat', label: t('order.totals.vat'), value: formatMoney(order.totalVatAmount) },
            {
              key: 'items',
              label: t('order.totals.items'),
              value: formatMoney(order.itemTotalAmount),
            },
            {
              key: 'discount',
              label: t('order.totals.discount'),
              value: formatMoney(order.headerDiscount),
            },
            { key: 'charges', label: t('order.totals.charges'), value: formatMoney(charges) },
            {
              key: 'net',
              label: t('order.totals.net'),
              value: formatMoney(order.netAmount),
              strong: true,
            },
          ]}
        />
      }
    />
  );
}
