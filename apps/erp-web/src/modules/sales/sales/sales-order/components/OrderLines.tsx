import { Field, MoneyInput, usePlaceholder } from '@erp/ui';
import { Form, Input, InputNumber } from 'antd';
import { useTranslation } from 'react-i18next';
import type { PayFrom } from '@/common/pay-from';
import { SalesLines } from '../../../components/SalesLines';
import { emptyLine, SHELF_LIFE_MAX, useSalesHeader } from '../../../sales-document';
import type { OrderLine } from '../order-form';

export function OrderLines({ payFrom }: { payFrom: PayFrom }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const header = useSalesHeader();
  const foreign = !!Form.useWatch('isForeign', form);
  payFrom.useClear();

  const rate = (line: number, key: 'fobRate' | 'freightRate', label: string) => (
    <Field name={[line, key]} label={label}>
      <MoneyInput compact className="erp-w-full" placeholder={placeholder('enter', label)} />
    </Field>
  );

  return (
    <SalesLines<OrderLine>
      description={t('salesOrder.lines.description')}
      newLine={emptyLine}
      header={header}
      fields={(line) => (
        <>
          <Field name={[line, 'shelfLife']} label={t('sales.lines.shelfLife')}>
            <Input
              maxLength={SHELF_LIFE_MAX}
              placeholder={placeholder('enter', t('sales.lines.shelfLife'))}
            />
          </Field>
          <Field name={[line, 'demandQuantity']} label={t('salesOrder.lines.demand')}>
            <InputNumber<number>
              min={0}
              controls={false}
              className="erp-w-full"
              placeholder={placeholder('enter', t('salesOrder.lines.demand'))}
            />
          </Field>
          {foreign && rate(line, 'fobRate', t('salesOrder.lines.fob'))}
          {foreign && rate(line, 'freightRate', t('salesOrder.lines.freight'))}
        </>
      )}
    />
  );
}
