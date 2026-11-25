import { Field } from '@erp/ui';
import { Form, Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { SalesLines } from '../../../components/SalesLines';
import { emptyLine } from '../../../sales-document';
import { DELIVERY_MAX } from '../constants';
import type { QuotationLine } from '../quotation-form';

export function QuotationLines() {
  const { t } = useTranslation();
  const form = Form.useFormInstance();
  const deliveryCharge: number | null = Form.useWatch('deliveryCharge', form);
  const extraCharge: number | null = Form.useWatch('extraCharge', form);

  return (
    <SalesLines<QuotationLine>
      offers={false}
      description={t('quotation.lines.description')}
      newLine={emptyLine}
      header={{
        isVatInclusive: Form.useWatch('isVatInclusive', form),
        isGrossDiscount: Form.useWatch('isGrossDiscount', form),
        discountPercentage: Form.useWatch('discountPercentage', form),
        discountAmount: Form.useWatch('discountAmount', form),
        charges: (deliveryCharge ?? 0) + (extraCharge ?? 0),
      }}
      fields={(line) => (
        <Field
          name={[line, 'deliveryDate']}
          label={t('quotation.lines.delivery')}
          className="erp-field-span-2"
        >
          <Input maxLength={DELIVERY_MAX} placeholder={t('quotation.lines.deliveryPlaceholder')} />
        </Field>
      )}
    />
  );
}
