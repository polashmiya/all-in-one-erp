import type { OrderDelivery } from '@erp/api-client';
import { DetailSection, formatDate, formatMoney, formatNumber, StatusTag } from '@erp/ui';
import { Space, Table } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { ApprovalStatusTag } from '@/common/documents/ApprovalStatusTag';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { deliveryPaths } from '../../sales-delivery/constants';

export function OrderDeliveries({ deliveries }: { deliveries: OrderDelivery[] }) {
  const { t } = useTranslation();

  return (
    <DetailSection title={t('salesOrder.deliveries')} description={t('salesOrder.deliveriesText')}>
      <Table<OrderDelivery>
        size="small"
        rowKey="id"
        pagination={false}
        dataSource={deliveries}
        columns={[
          {
            key: 'code',
            title: t('salesOrder.delivery'),
            render: (_, row) => <Link to={deliveryPaths.view(row.id)}>{row.code}</Link>,
          },
          {
            key: 'date',
            title: t('salesOrder.deliveryDate'),
            render: (_, row) => formatDate(row.deliveryDate),
          },
          {
            key: 'quantity',
            title: t('document.lines.quantity'),
            align: 'end',
            render: (_, row) => formatNumber(row.totalQuantity, QUANTITY_DECIMALS_SHOWN),
          },
          {
            key: 'amount',
            title: t('salesOrder.amount'),
            align: 'end',
            render: (_, row) => formatMoney(row.netAmount),
          },
          {
            key: 'status',
            title: t('document.col.status'),
            render: (_, row) => (
              <Space>
                <ApprovalStatusTag status={row.status} />
                {row.isConfirmed && (
                  <StatusTag tone="info">{t('salesDelivery.confirmed')}</StatusTag>
                )}
              </Space>
            ),
          },
        ]}
      />
    </DetailSection>
  );
}
