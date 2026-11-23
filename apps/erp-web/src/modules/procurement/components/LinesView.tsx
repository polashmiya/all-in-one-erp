import { DetailSection, formatNumber, TitleCell } from '@erp/ui';
import { Table, type TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from '../procurement';
import { LineTotals, type LineTotal } from './LineTotals';

interface ViewLine {
  id: number;
  itemCode: string;
  itemName: string;
  uomName: string;
  quantity: number;
}

export function LinesView<T extends ViewLine>({
  rows,
  columns,
  totals,
}: {
  rows: T[];
  columns: TableColumnsType<T>;
  totals: LineTotal[];
}) {
  const { t } = useTranslation();

  const allColumns: TableColumnsType<T> = [
    {
      key: 'item',
      title: t('procurement.lines.item'),
      render: (_, line) => <TitleCell title={line.itemName} subtitle={line.itemCode} />,
    },
    { key: 'uom', title: t('procurement.lines.uom'), render: (_, line) => line.uomName },
    {
      key: 'quantity',
      title: t('procurement.lines.quantity'),
      align: 'end',
      render: (_, line) => formatNumber(line.quantity, QUANTITY_DECIMALS_SHOWN),
    },
    ...columns,
  ];

  return (
    <DetailSection
      title={t('procurement.lines.title')}
      description={t('procurement.itemCount', { count: rows.length })}
    >
      <div className="erp-lines">
        <Table<T>
          size="small"
          rowKey="id"
          pagination={false}
          dataSource={rows}
          columns={allColumns}
          scroll={{ x: 'max-content' }}
        />
        <div className="erp-lines__footer">
          <span />
          <LineTotals items={totals} />
        </div>
      </div>
    </DetailSection>
  );
}
