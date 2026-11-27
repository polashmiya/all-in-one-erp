import type { SalesDeliveryDetail, SalesOrderDetail } from '@erp/api-client';
import {
  DescriptionList,
  formatMoney,
  formatNumber,
  formatPercent,
  StatusTag,
  type DescriptionItem,
} from '@erp/ui';
import type { TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { LinesView } from '@/common/documents/LinesView';

type SalesRecord = SalesOrderDetail | SalesDeliveryDetail;

export function SalesLinesView<R extends SalesRecord>({
  record,
  columns = [],
  details,
}: {
  record: R;
  columns?: TableColumnsType<R['lines'][number]>;
  details: (line: R['lines'][number]) => DescriptionItem[];
}) {
  const { t } = useTranslation();
  const rows: R['lines'][number][] = record.lines;
  const charges = record.othersCost + record.extraCharge;
  const reductions = record.specialDiscountAmount + record.deductionValue;
  const tax = (percentage: number, amount: number) =>
    `${formatPercent(percentage)} · ${formatMoney(amount)}`;
  const money = (key: string, label: string, value: number) => ({
    key,
    label,
    value: formatMoney(value),
  });

  return (
    <LinesView<R['lines'][number]>
      rows={rows}
      columns={[
        ...columns,
        {
          key: 'price',
          title: t('document.lines.price'),
          align: 'end',
          render: (_, line) =>
            line.isOfferItem ? (
              <StatusTag tone="success">{t('sales.lines.freeItem')}</StatusTag>
            ) : (
              formatMoney(line.price)
            ),
        },
        {
          key: 'discount',
          title: t('document.lines.discount'),
          align: 'end',
          render: (_, line) =>
            line.discountPercentage
              ? tax(line.discountPercentage, line.discountAmount)
              : formatMoney(line.discountAmount),
        },
        {
          key: 'amount',
          title: t('sales.lines.amount'),
          align: 'end',
          render: (_, line) => formatMoney(line.totalValue),
        },
      ]}
      details={(line) => (
        <DescriptionList
          items={[
            {
              key: 'sd',
              label: t('sales.lines.sd'),
              value: tax(line.sdPercentage, line.sdAmount),
            },
            {
              key: 'vat',
              label: t('document.lines.vat'),
              value: tax(line.vatPercentage, line.vatAmount),
            },
            {
              key: 'ait',
              label: t('sales.lines.ait'),
              value: tax(line.aitPercentage, line.aitAmount),
            },
            {
              key: 'warranty',
              label: t('sales.lines.warranty'),
              value: formatNumber(line.warrantyInMonth),
            },
            { key: 'shelfLife', label: t('sales.lines.shelfLife'), value: line.shelfLife },
            ...details(line),
            {
              key: 'description',
              label: t('sales.lines.itemDescription'),
              value: line.itemDescription,
              wide: true,
            },
          ]}
        />
      )}
      totals={[
        {
          key: 'quantity',
          label: t('document.lines.totalQuantity'),
          value: formatNumber(record.totalQuantity, QUANTITY_DECIMALS_SHOWN),
        },
        money('gross', t('sales.totals.gross'), record.itemTotalAmount),
        money(
          'discount',
          t('document.totals.discount'),
          record.headerDiscount + record.totalLineDiscount,
        ),
        money('sd', t('sales.totals.sd'), record.totalSd),
        money('vat', t('document.totals.vat'), record.totalVat),
        money('ait', t('sales.totals.ait'), record.totalAit),
        money('charges', t('document.totals.charges'), charges),
        money('reductions', t('sales.totals.reductions'), reductions),
        { ...money('total', t('sales.totals.total'), record.netAmount), strong: true },
      ]}
    />
  );
}
