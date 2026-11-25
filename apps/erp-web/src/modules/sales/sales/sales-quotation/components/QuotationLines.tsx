import {
  Field,
  FieldGrid,
  formatMoney,
  formatNumber,
  MoneyInput,
  PercentInput,
  usePlaceholder,
} from '@erp/ui';
import { Form, Input, InputNumber } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN, VAT_MAX } from '@/common/documents/documents';
import { LineGrid } from '@/common/documents/LineGrid';
import { salesPrices, salesTotals, type SalesHeader } from '../../../pricing';
import { DELIVERY_MAX, DESCRIPTION_MAX, SD_MAX, WARRANTY_MAX } from '../constants';
import { emptyLine, type QuotationLine } from '../quotation-form';
import { DiscountInput } from './DiscountInput';
import './quotation.css';

export function QuotationLines() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const lines: QuotationLine[] = Form.useWatch('lines', form) ?? [];
  const header: SalesHeader = {
    isVatInclusive: Form.useWatch('isVatInclusive', form),
    isGrossDiscount: Form.useWatch('isGrossDiscount', form),
    discountPercentage: Form.useWatch('discountPercentage', form),
    discountAmount: Form.useWatch('discountAmount', form),
    deliveryCharge: Form.useWatch('deliveryCharge', form),
    extraCharge: Form.useWatch('extraCharge', form),
  };
  const prices = salesPrices(lines, header);

  const percent = (
    line: number,
    key: 'sdPercentage' | 'vatPercentage' | 'aitPercentage',
    label: string,
    max: number,
  ) => (
    <Field name={[line, key]} label={label}>
      <PercentInput max={max} className="erp-w-full" placeholder={placeholder('enter', label)} />
    </Field>
  );

  return (
    <LineGrid<QuotationLine>
      saleable
      description={t('quotation.lines.description')}
      newLine={emptyLine}
      onPick={(option) => ({
        price: option.salesPrice,
        sdPercentage: option.sdPercentage,
        vatPercentage: option.salesVatPercentage,
      })}
      note={{
        name: 'itemDescription',
        placeholder: t('quotation.lines.descriptionPlaceholder'),
        maxLength: DESCRIPTION_MAX,
      }}
      details={({ name }) => (
        <FieldGrid columns={6}>
          {percent(name, 'sdPercentage', t('quotation.lines.sd'), SD_MAX)}
          {percent(name, 'vatPercentage', t('document.lines.vat'), VAT_MAX)}
          {percent(name, 'aitPercentage', t('quotation.lines.ait'), VAT_MAX)}
          <Field name={[name, 'warrantyInMonth']} label={t('quotation.lines.warranty')}>
            <InputNumber<number>
              min={0}
              max={WARRANTY_MAX}
              precision={0}
              controls={false}
              className="erp-w-full"
              placeholder={placeholder('enter', t('quotation.lines.warranty'))}
            />
          </Field>
          <Field
            name={[name, 'deliveryDate']}
            label={t('quotation.lines.delivery')}
            className="erp-field-span-2"
          >
            <Input
              maxLength={DELIVERY_MAX}
              placeholder={t('quotation.lines.deliveryPlaceholder')}
            />
          </Field>
        </FieldGrid>
      )}
      columns={[
        {
          key: 'price',
          title: t('document.lines.price'),
          className: 'erp-lines__money',
          align: 'end',
          render: (field) => (
            <Form.Item name={[field.name, 'price']}>
              <MoneyInput
                compact
                className="erp-w-full"
                placeholder={placeholder('enter', t('document.lines.price'))}
              />
            </Form.Item>
          ),
        },
        {
          key: 'discount',
          title: t('document.lines.discount'),
          className: 'erp-quotation__discount',
          align: 'end',
          render: (field) =>
            header.isGrossDiscount ? (
              formatMoney(prices[field.name]?.discount)
            ) : (
              <DiscountInput line={field.name} />
            ),
        },
        {
          key: 'amount',
          title: t('quotation.lines.amount'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (field) => formatMoney(prices[field.name]?.value),
        },
      ]}
      totals={(rows) => {
        const totals = salesTotals(rows, header);
        return [
          {
            key: 'quantity',
            label: t('document.lines.totalQuantity'),
            value: formatNumber(totals.quantity, QUANTITY_DECIMALS_SHOWN),
          },
          { key: 'gross', label: t('quotation.totals.gross'), value: formatMoney(totals.gross) },
          {
            key: 'discount',
            label: t('document.totals.discount'),
            value: formatMoney(totals.discount),
          },
          { key: 'sd', label: t('quotation.totals.sd'), value: formatMoney(totals.sd) },
          { key: 'vat', label: t('document.totals.vat'), value: formatMoney(totals.vat) },
          { key: 'ait', label: t('quotation.totals.ait'), value: formatMoney(totals.ait) },
          {
            key: 'charges',
            label: t('document.totals.charges'),
            value: formatMoney(totals.charges),
          },
          {
            key: 'total',
            label: t('quotation.totals.total'),
            value: formatMoney(totals.total),
            strong: true,
          },
        ];
      }}
    />
  );
}
