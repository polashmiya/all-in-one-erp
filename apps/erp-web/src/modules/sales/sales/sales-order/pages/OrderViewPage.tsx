import {
  CalendarOutlined,
  CarOutlined,
  FileTextOutlined,
  HomeOutlined,
  IdcardOutlined,
  ShoppingOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { getGetSalesOrderQueryKey, useGetSalesOrder, type SalesOrderDetail } from '@erp/api-client';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  formatNumber,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { DocumentView } from '@/common/documents/DocumentView';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { SalesLinesView } from '../../../components/SalesLinesView';
import { DELIVERY_FEATURE, deliveryPaths } from '../../sales-delivery/constants';
import { OrderDeliveries } from '../components/OrderDeliveries';
import { ORDER_FEATURE, orderPaths } from '../constants';
import { orderCalls } from '../order-form';

export function OrderViewPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const id = Number(useParams().id);
  const access = useFeatureAccess(ORDER_FEATURE);
  const deliveries = useFeatureAccess(DELIVERY_FEATURE);
  const config = useFeatureConfig(ORDER_FEATURE, access.view);
  const query = useGetSalesOrder(id, {
    query: { queryKey: useUnitKey(getGetSalesOrderQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const order = query.data;
  if (!order || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('salesOrder.notFound')}
        missingActions={[{ key: 'back', label: t('salesOrder.backToList'), to: orderPaths.list }]}
      />
    );
  }

  const quantity = (value: number) => formatNumber(value, QUANTITY_DECIMALS_SHOWN);
  const money = (key: string, label: string, value: number) => ({
    key,
    label,
    value: formatMoney(value),
  });

  return (
    <DocumentView<SalesOrderDetail>
      document={order}
      config={config.data}
      feature={ORDER_FEATURE}
      paths={orderPaths}
      calls={orderCalls}
      icon={<ShoppingOutlined />}
      tag={order.isCompleted ? t('salesOrder.completed') : order.paymentTypeName}
      meta={[
        { key: 'customer', icon: <UserOutlined />, label: order.customerName },
        {
          key: 'dates',
          icon: <CalendarOutlined />,
          label: [
            t('salesOrder.meta.ordered', { date: formatDate(order.orderDate) }),
            t('salesOrder.meta.delivery', { date: formatDate(order.deliveryDate) }),
          ].join(' · '),
        },
        {
          key: 'delivered',
          icon: <CarOutlined />,
          label: t('salesOrder.deliveredOf', {
            delivered: quantity(order.deliveredQuantity),
            total: quantity(order.totalQuantity),
          }),
        },
        { key: 'quotation', icon: <FileTextOutlined />, label: order.salesQuotationName },
        { key: 'warehouse', icon: <HomeOutlined />, label: order.warehouseName },
        { key: 'salesForce', icon: <IdcardOutlined />, label: order.salesForceName },
      ]}
      renderers={{ shippingAddressId: (record) => record.shipToName ?? '—' }}
      moreActions={[
        {
          key: 'deliver',
          label: t('salesOrder.deliver'),
          icon: <CarOutlined />,
          hidden: !deliveries.create || order.status !== 'approved' || order.isCompleted,
          onClick: () => navigate(deliveryPaths.fromOrder(order.id)),
        },
      ]}
      sections={[
        {
          key: 'money',
          content: (
            <DetailSection title={t('salesOrder.money')}>
              <DescriptionList
                items={[
                  money('net', t('sales.totals.total'), order.netAmount),
                  money('received', t('salesOrder.received'), order.receiveAmount),
                  money('pending', t('salesOrder.pending'), order.pendingAmount),
                  money('advance', t('salesOrder.advance'), order.advanceAmount),
                  money('adjusted', t('salesOrder.adjusted'), order.adjustedAmount),
                  money('left', t('salesOrder.advanceLeft'), order.advanceRemainingAmount),
                ]}
              />
            </DetailSection>
          ),
        },
        ...(order.deliveries.length
          ? [{ key: 'deliveries', content: <OrderDeliveries deliveries={order.deliveries} /> }]
          : []),
      ]}
      lines={
        <SalesLinesView<SalesOrderDetail>
          record={order}
          columns={[
            {
              key: 'delivered',
              title: t('salesOrder.lines.delivered'),
              align: 'end',
              render: (_, line) => quantity(line.deliveredQuantity),
            },
          ]}
          details={(line) => [
            {
              key: 'scheduled',
              label: t('salesOrder.lines.scheduled'),
              value: quantity(line.scheduledQuantity),
            },
            {
              key: 'demand',
              label: t('salesOrder.lines.demand'),
              value: line.demandQuantity === null ? null : quantity(line.demandQuantity),
            },
            ...(order.isForeign
              ? [
                  money('fob', t('salesOrder.lines.fob'), line.fobRate),
                  money('freight', t('salesOrder.lines.freight'), line.freightRate),
                ]
              : []),
          ]}
        />
      }
    />
  );
}
