import {
  Field,
  FieldGrid,
  formatMoney,
  formatNumber,
  MoneyInput,
  PercentInput,
  StatusTag,
  usePlaceholder,
} from '@erp/ui';
import { Form, InputNumber, Switch } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN, VAT_MAX } from '@/common/documents/documents';
import { LineGrid, type LineColumn } from '@/common/documents/LineGrid';
import type { LineTotal } from '@/common/documents/LineTotals';
import { salesPrices, salesTotals, type SalesHeader } from '../pricing';
import { DESCRIPTION_MAX, SD_MAX, WARRANTY_MAX, type SalesLineValue } from '../sales-document';
import { DiscountInput } from './DiscountInput';
import './sales.css';

function LockedPrice({ line }: { line: SalesLineValue | undefined }) {
  const { t } = useTranslation();
  return line?.isOfferItem ? (
    <StatusTag tone="success">{t('sales.lines.freeItem')}</StatusTag>
  ) : (
    formatMoney(line?.price)
  );
}

export function SalesLines<L extends SalesLineValue>({
  description,
  header,
  newLine,
  fields,
  columns = [],
  offers = true,
  locked,
  warranty = true,
  settlement = [],
}: {
  description: string;
  header: SalesHeader;
  newLine: () => Partial<L>;
  fields: (line: number) => ReactNode;
  columns?: LineColumn<L>[];
  offers?: boolean;
  locked?: boolean;
  warranty?: boolean;
  settlement?: LineTotal[];
}) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const lines: L[] = Form.useWatch('lines', form) ?? [];
  const prices = salesPrices(lines, header);

  const percent = (
    line: number,
    key: 'sdPercentage' | 'vatPercentage' | 'aitPercentage',
    label: string,
    max: number,
  ) => (
    <Field name={[line, key]} label={label}>
      <PercentInput
        max={max}
        disabled={locked}
        className="erp-w-full"
        placeholder={placeholder('enter', label)}
      />
    </Field>
  );

  const free: LineColumn<L> = {
    key: 'free',
    title: t('sales.lines.free'),
    render: (field) => (
      <Form.Item name={[field.name, 'isOfferItem']} valuePropName="checked">
        <Switch size="small" aria-label={t('sales.lines.freeItem')} />
      </Form.Item>
    ),
  };

  return (
    <LineGrid<L>
      saleable
      fixed={locked}
      description={description}
      newLine={newLine}
      onPick={(option) =>
        ({
          price: option.salesPrice,
          sdPercentage: option.sdPercentage,
          vatPercentage: option.salesVatPercentage,
        }) as Partial<L>
      }
      note={{
        name: 'itemDescription' as Extract<keyof L, string>,
        placeholder: t('sales.lines.descriptionPlaceholder'),
        maxLength: DESCRIPTION_MAX,
      }}
      details={({ name }) => (
        <FieldGrid>
          {percent(name, 'sdPercentage', t('sales.lines.sd'), SD_MAX)}
          {percent(name, 'vatPercentage', t('document.lines.vat'), VAT_MAX)}
          {percent(name, 'aitPercentage', t('sales.lines.ait'), VAT_MAX)}
          {warranty && (
            <Field name={[name, 'warrantyInMonth']} label={t('sales.lines.warranty')}>
              <InputNumber<number>
                min={0}
                max={WARRANTY_MAX}
                precision={0}
                controls={false}
                className="erp-w-full"
                placeholder={placeholder('enter', t('sales.lines.warranty'))}
              />
            </Field>
          )}
          {fields(name)}
        </FieldGrid>
      )}
      columns={[
        ...columns,
        ...(offers && !locked ? [free] : []),
        {
          key: 'price',
          title: t('document.lines.price'),
          className: locked ? 'erp-lines__amount' : 'erp-lines__money',
          align: 'end',
          render: (field, line) =>
            locked ? (
              <LockedPrice line={line} />
            ) : (
              <Form.Item name={[field.name, 'price']}>
                <MoneyInput
                  compact
                  disabled={line?.isOfferItem}
                  className="erp-w-full"
                  placeholder={placeholder('enter', t('document.lines.price'))}
                />
              </Form.Item>
            ),
        },
        {
          key: 'discount',
          title: t('document.lines.discount'),
          className: locked ? 'erp-lines__amount' : 'erp-sales__discount',
          align: 'end',
          render: (field, line) =>
            locked || header.isGrossDiscount || line?.isOfferItem ? (
              formatMoney(prices[field.name]?.discount)
            ) : (
              <DiscountInput line={field.name} />
            ),
        },
        {
          key: 'amount',
          title: t('sales.lines.amount'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (field) => formatMoney(prices[field.name]?.value),
        },
      ]}
      totals={(rows) => {
        const totals = salesTotals(rows, header);
        const money = (key: string, label: string, value: number) => ({
          key,
          label,
          value: formatMoney(value),
        });
        return [
          {
            key: 'quantity',
            label: t('document.lines.totalQuantity'),
            value: formatNumber(totals.quantity, QUANTITY_DECIMALS_SHOWN),
          },
          money('gross', t('sales.totals.gross'), totals.gross),
          money('discount', t('document.totals.discount'), totals.discount),
          money('sd', t('sales.totals.sd'), totals.sd),
          money('vat', t('document.totals.vat'), totals.vat),
          money('ait', t('sales.totals.ait'), totals.ait),
          money('charges', t('document.totals.charges'), totals.charges),
          ...(header.reductions === undefined
            ? []
            : [money('reductions', t('sales.totals.reductions'), totals.reductions)]),
          { ...money('total', t('sales.totals.total'), totals.total), strong: true },
          ...settlement,
        ];
      }}
    />
  );
}
