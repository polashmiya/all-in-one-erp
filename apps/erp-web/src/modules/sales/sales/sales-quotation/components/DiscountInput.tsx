import { CURRENCY_SYMBOL, usePlaceholder } from '@erp/ui';
import { Form, InputNumber, Select, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import type { DiscountType } from '../../../pricing';

export function DiscountInput({ line }: { line: number }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const options: { value: DiscountType; label: string }[] = [
    { value: 'percent', label: '%' },
    { value: 'amount', label: CURRENCY_SYMBOL },
    { value: 'unit', label: t('quotation.lines.perUnit', { symbol: CURRENCY_SYMBOL }) },
  ];

  return (
    <Space.Compact className="erp-w-full">
      <Form.Item name={[line, 'discount']} noStyle>
        <InputNumber<number>
          min={0}
          precision={2}
          controls={false}
          className="erp-w-full"
          placeholder={placeholder('enter', t('document.lines.discount'))}
        />
      </Form.Item>
      <Form.Item name={[line, 'discountType']} noStyle>
        <Select<DiscountType>
          className="erp-quotation__discount-type"
          aria-label={t('quotation.lines.discountType')}
          options={options}
          popupMatchSelectWidth={false}
        />
      </Form.Item>
    </Space.Compact>
  );
}
