import type { RequisitionDetail, RequisitionLine } from '@erp/api-client';
import { DetailSection, formatMoney, formatNumber, TitleCell } from '@erp/ui';
import { Table, type TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DISPLAY_DECIMALS } from '../constants';

export function RequisitionLinesView({ requisition }: { requisition: RequisitionDetail }) {
  const { t } = useTranslation();

  const columns: TableColumnsType<RequisitionLine> = [
    {
      key: 'item',
      title: t('requisition.lines.item'),
      render: (_, line) => <TitleCell title={line.itemName} subtitle={line.itemCode} />,
    },
    { key: 'uom', title: t('requisition.lines.uom'), render: (_, line) => line.uomName },
    {
      key: 'quantity',
      title: t('requisition.lines.quantity'),
      align: 'end',
      render: (_, line) => formatNumber(line.quantity, QUANTITY_DISPLAY_DECIMALS),
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
  ];

  return (
    <DetailSection
      title={t('requisition.lines.title')}
      description={t('requisition.itemCount', { count: requisition.lines.length })}
    >
      <Table<RequisitionLine>
        size="small"
        rowKey="id"
        pagination={false}
        dataSource={requisition.lines}
        columns={columns}
        scroll={{ x: 'max-content' }}
        summary={() => (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={2}>
              <strong>{t('requisition.lines.total')}</strong>
            </Table.Summary.Cell>
            <Table.Summary.Cell index={2} align="end">
              <strong>{formatNumber(requisition.totalQuantity, QUANTITY_DISPLAY_DECIMALS)}</strong>
            </Table.Summary.Cell>
            <Table.Summary.Cell index={3} />
            <Table.Summary.Cell index={4} align="end">
              <strong>{formatMoney(requisition.totalAmount)}</strong>
            </Table.Summary.Cell>
            <Table.Summary.Cell index={5} />
          </Table.Summary.Row>
        )}
      />
    </DetailSection>
  );
}
