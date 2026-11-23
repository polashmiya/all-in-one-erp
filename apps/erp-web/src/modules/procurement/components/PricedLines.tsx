import type { ProcurementItemOption } from '@erp/api-client';
import { formatMoney, formatNumber, MoneyInput, PercentInput, usePlaceholder } from '@erp/ui';
import { Form } from 'antd';
import { useTranslation } from 'react-i18next';
import { pricedTotals, priced, type PricedLine } from '../pricing';
import { QUANTITY_DECIMALS_SHOWN, VAT_MAX } from '../procurement';
import { LineGrid, type BaseLine, type LineColumn } from './LineGrid';

export function PricedLines<L extends BaseLine & PricedLine>({
  description,
  note,
  newLine,
  onPick,
  fixed,
  columns = [],
}: {
  description: string;
  note: { name: Extract<keyof L, string>; placeholder: string; maxLength: number };
  newLine: () => Partial<L>;
  onPick: (option: ProcurementItemOption) => Partial<L>;
  fixed?: boolean;
  columns?: LineColumn<L>[];
}) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const vatInclusive = !!Form.useWatch('isVatInclusive', form);
  const headerDiscount: number = Form.useWatch('headerDiscount', form) ?? 0;
  const charges =
    (Form.useWatch('deliveryCharge', form) ?? 0) +
    (Form.useWatch('laborCharge', form) ?? 0) +
    (Form.useWatch('otherCharge', form) ?? 0);

  const money = (key: 'price' | 'discount') => ({
    key,
    title: t(`procurement.lines.${key}`),
    className: 'erp-lines__money',
    align: 'end' as const,
    render: (field: { name: number }) => (
      <Form.Item name={[field.name, key]}>
        <MoneyInput
          compact
          className="erp-w-full"
          placeholder={placeholder('enter', t(`procurement.lines.${key}`))}
        />
      </Form.Item>
    ),
  });

  return (
    <LineGrid<L>
      description={description}
      newLine={newLine}
      onPick={onPick}
      fixed={fixed}
      note={note}
      columns={[
        ...columns,
        money('price'),
        money('discount'),
        {
          key: 'vat',
          title: t('procurement.lines.vat'),
          className: 'erp-lines__number',
          align: 'end',
          render: (field) => (
            <Form.Item name={[field.name, 'vatPercentage']}>
              <PercentInput
                max={VAT_MAX}
                className="erp-w-full"
                placeholder={placeholder('enter', t('procurement.lines.vat'))}
              />
            </Form.Item>
          ),
        },
        {
          key: 'net',
          title: t('procurement.lines.net'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (_, line) => formatMoney(priced(line, vatInclusive).net),
        },
      ]}
      totals={(lines) => {
        const totals = pricedTotals(lines, {
          isVatInclusive: vatInclusive,
          headerDiscount,
          charges,
        });
        return [
          {
            key: 'quantity',
            label: t('procurement.lines.totalQuantity'),
            value: formatNumber(totals.quantity, QUANTITY_DECIMALS_SHOWN),
          },
          { key: 'vat', label: t('procurement.totals.vat'), value: formatMoney(totals.vat) },
          {
            key: 'items',
            label: t('procurement.totals.items'),
            value: formatMoney(totals.itemTotal),
          },
          {
            key: 'discount',
            label: t('procurement.totals.discount'),
            value: formatMoney(headerDiscount),
          },
          { key: 'charges', label: t('procurement.totals.charges'), value: formatMoney(charges) },
          {
            key: 'net',
            label: t('procurement.totals.net'),
            value: formatMoney(totals.net),
            strong: true,
          },
        ];
      }}
    />
  );
}
