import type { DeliveryFollowUp } from '@erp/api-client';
import { DetailSection, formatDate, formatMoney } from '@erp/ui';
import { Table } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { ApprovalStatusTag } from '@/common/documents/ApprovalStatusTag';
import { documentLink } from '@/modules/document-registry';

export function DeliveryFollowUps({ rows }: { rows: DeliveryFollowUp[] }) {
  const { t } = useTranslation();

  return (
    <DetailSection
      title={t('salesDelivery.followUps')}
      description={t('salesDelivery.followUpsText')}
    >
      <Table<DeliveryFollowUp>
        size="small"
        rowKey={(row) => `${row.feature}:${row.id}`}
        pagination={false}
        dataSource={rows}
        columns={[
          {
            key: 'type',
            title: t('salesDelivery.followUp.type'),
            render: (_, row) => t(documentLink(row.feature)?.label ?? row.feature),
          },
          {
            key: 'code',
            title: t('salesDelivery.followUp.code'),
            render: (_, row) => (
              <Link to={documentLink(row.feature)?.path(row.id) ?? ''}>{row.code}</Link>
            ),
          },
          {
            key: 'date',
            title: t('salesDelivery.followUp.date'),
            render: (_, row) => formatDate(row.date),
          },
          {
            key: 'amount',
            title: t('salesDelivery.followUp.amount'),
            align: 'end',
            render: (_, row) => formatMoney(row.amount),
          },
          {
            key: 'status',
            title: t('document.col.status'),
            render: (_, row) => <ApprovalStatusTag status={row.status} />,
          },
        ]}
      />
    </DetailSection>
  );
}
