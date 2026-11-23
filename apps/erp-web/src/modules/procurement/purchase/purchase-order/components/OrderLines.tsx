import { formatMoney, formatNumber, MoneyInput, PercentInput, usePlaceholder } from '@erp/ui';
import { Form, Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { LineGrid } from '../../../components/LineGrid';
import { QUANTITY_DECIMALS_SHOWN } from '../../../procurement';
import { SPECIFICATION_MAX, VAT_MAX } from '../constants';
import { emptyLine, orderTotals, priced, type OrderLine, type OrderValues } from '../order-form';

export function OrderLines() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance<OrderValues>();
  const vatInclusive = !!Form.useWatch('isVatInclusive', form);
  const headerDiscount = Form.useWatch('headerDiscount', form) ?? 0;
  const charges =
    (Form.useWatch('deliveryCharge', form) ?? 0) +
    (Form.useWatch('laborCharge', form) ?? 0) +
    (Form.useWatch('otherCharge', form) ?? 0);

  return (
    <LineGrid<OrderLine>
      description={t('order.lines.description')}
      newLine={emptyLine}
      onPick={(option) => ({ price: option.rate, vatPercentage: option.vatPercentage })}
      columns={[
        {
          key: 'price',
          title: t('order.lines.price'),
          className: 'erp-lines__money',
          align: 'end',
          render: (field) => (
            <Form.Item name={[field.name, 'price']}>
              <MoneyInput
                className="erp-w-full"
                placeholder={placeholder('enter', t('order.lines.price'))}
              />
            </Form.Item>
          ),
        },
        {
          key: 'discount',
          title: t('order.lines.discount'),
          className: 'erp-lines__money',
          align: 'end',
          render: (field) => (
            <Form.Item name={[field.name, 'discount']}>
              <MoneyInput
                className="erp-w-full"
                placeholder={placeholder('enter', t('order.lines.discount'))}
              />
            </Form.Item>
          ),
        },
        {
          key: 'vat',
          title: t('order.lines.vat'),
          className: 'erp-lines__number',
          align: 'end',
          render: (field) => (
            <Form.Item name={[field.name, 'vatPercentage']}>
              <PercentInput
                max={VAT_MAX}
                className="erp-w-full"
                placeholder={placeholder('enter', t('order.lines.vat'))}
              />
            </Form.Item>
          ),
        },
        {
          key: 'net',
          title: t('order.lines.net'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (_, line) => formatMoney(priced(line, vatInclusive).net),
        },
        {
          key: 'specification',
          title: t('order.lines.specification'),
          className: 'erp-lines__text',
          render: (field) => (
            <Form.Item name={[field.name, 'specification']}>
              <Input
                maxLength={SPECIFICATION_MAX}
                placeholder={t('order.lines.specificationPlaceholder')}
              />
            </Form.Item>
          ),
        },
      ]}
      totals={(lines) => {
        const totals = orderTotals(lines, {
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
          { key: 'vat', label: t('order.totals.vat'), value: formatMoney(totals.vat) },
          { key: 'items', label: t('order.totals.items'), value: formatMoney(totals.itemTotal) },
          {
            key: 'discount',
            label: t('order.totals.discount'),
            value: formatMoney(headerDiscount),
          },
          { key: 'charges', label: t('order.totals.charges'), value: formatMoney(charges) },
          {
            key: 'net',
            label: t('order.totals.net'),
            value: formatMoney(totals.net),
            strong: true,
          },
        ];
      }}
    />
  );
}
