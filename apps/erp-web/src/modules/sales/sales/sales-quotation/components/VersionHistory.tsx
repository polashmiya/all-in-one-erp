import type { SalesQuotationVersion } from '@erp/api-client';
import { DetailSection, formatDate, formatMoney } from '@erp/ui';
import { Table } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { ApprovalStatusTag } from '@/common/documents/ApprovalStatusTag';
import { quotationPaths } from '../constants';

export function VersionHistory({
  versions,
  currentId,
}: {
  versions: SalesQuotationVersion[];
  currentId: number;
}) {
  const { t } = useTranslation();

  return (
    <DetailSection title={t('quotation.versions')} description={t('quotation.versionsText')}>
      <Table<SalesQuotationVersion>
        size="small"
        rowKey="id"
        pagination={false}
        dataSource={versions}
        columns={[
          {
            key: 'version',
            title: t('quotation.version'),
            render: (_, row) =>
              row.id === currentId ? (
                <strong>{row.version}</strong>
              ) : (
                <Link to={quotationPaths.view(row.id)}>{row.version}</Link>
              ),
          },
          {
            key: 'date',
            title: t('quotation.versionDate'),
            render: (_, row) => formatDate(row.versionDate),
          },
          {
            key: 'total',
            title: t('quotation.totals.total'),
            align: 'end',
            render: (_, row) => formatMoney(row.totalAmount),
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
