import { DetailSection, formatNumber, TitleCell } from '@erp/ui';
import { Table, type TableColumnsType } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from './documents';
import { LineTotals, type LineTotal } from './LineTotals';
import './documents.css';

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
  details,
}: {
  rows: T[];
  columns: TableColumnsType<T>;
  totals: LineTotal[];
  details?: (line: T) => ReactNode;
}) {
  const { t } = useTranslation();

  const allColumns: TableColumnsType<T> = [
    {
      key: 'item',
      title: t('document.lines.item'),
      render: (_, line) => <TitleCell title={line.itemName} subtitle={line.itemCode} />,
    },
    { key: 'uom', title: t('document.lines.uom'), render: (_, line) => line.uomName },
    {
      key: 'quantity',
      title: t('document.lines.quantity'),
      align: 'end',
      render: (_, line) => formatNumber(line.quantity, QUANTITY_DECIMALS_SHOWN),
    },
    ...columns,
  ];

  return (
    <DetailSection
      title={t('document.lines.title')}
      description={t('document.itemCount', { count: rows.length })}
    >
      <div className="erp-lines">
        <Table<T>
          size="small"
          rowKey="id"
          pagination={false}
          dataSource={rows}
          columns={allColumns}
          scroll={{ x: 'max-content' }}
          expandable={
            details && {
              expandedRowRender: (line) => (
                <div className="erp-lines__details">{details(line)}</div>
              ),
              expandedRowKeys: rows.map((line) => line.id),
              showExpandColumn: false,
            }
          }
        />
        <div className="erp-lines__footer">
          <span />
          <LineTotals items={totals} />
        </div>
      </div>
    </DetailSection>
  );
}
