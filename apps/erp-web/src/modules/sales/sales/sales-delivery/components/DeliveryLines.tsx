import { Field, FieldInput, formatNumber, usePlaceholder } from '@erp/ui';
import { Form, Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import type { LineColumn } from '@/common/documents/LineGrid';
import { useOnHand } from '@/common/documents/useOnHand';
import type { PayFrom } from '@/common/pay-from';
import { SalesLines } from '../../../components/SalesLines';
import { emptyLine, SHELF_LIFE_MAX, useSalesHeader } from '../../../sales-document';
import { BATCH_MAX, FROM_ORDER, PACK_MAX, SERIAL_MAX, WARRANTY_TEXT_MAX } from '../constants';
import type { DeliveryLine } from '../delivery-form';

export function DeliveryLines({ payFrom }: { payFrom: PayFrom }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const header = useSalesHeader();
  const ordered = Form.useWatch('type', form) === FROM_ORDER;
  const warehouseId: string | undefined = Form.useWatch('warehouseId', form);
  const lines: DeliveryLine[] = Form.useWatch('lines', form) ?? [];
  const onHand = useOnHand(
    warehouseId,
    lines.flatMap((line) => line?.itemId ?? []),
  );
  payFrom.useClear();

  const count = (key: string, value: (line?: DeliveryLine) => number | null | undefined) =>
    ({
      key,
      title: t(`salesDelivery.lines.${key}`),
      className: 'erp-lines__amount',
      align: 'end',
      render: (_, line) => {
        const quantity = value(line);
        return quantity === null || quantity === undefined
          ? '—'
          : formatNumber(quantity, QUANTITY_DECIMALS_SHOWN);
      },
    }) satisfies LineColumn<DeliveryLine>;

  const text = (line: number, key: keyof DeliveryLine, label: string, maxLength: number) => (
    <Field name={[line, key]} label={label}>
      <Input maxLength={maxLength} placeholder={placeholder('enter', label)} />
    </Field>
  );

  return (
    <SalesLines<DeliveryLine>
      locked={ordered}
      description={t(
        ordered ? 'salesDelivery.lines.orderDescription' : 'salesDelivery.lines.directDescription',
      )}
      newLine={emptyLine}
      header={header}
      columns={[
        ...(ordered ? [count('remaining', (line) => line?.remaining)] : []),
        count('stock', (line) => onHand(line?.itemId)?.quantity),
      ]}
      fields={(line) => (
        <>
          {text(line, 'batch', t('salesDelivery.lines.batch'), BATCH_MAX)}
          {text(line, 'itemSerial', t('salesDelivery.lines.serial'), SERIAL_MAX)}
          {text(line, 'packQuantity', t('salesDelivery.lines.pack'), PACK_MAX)}
          {text(line, 'shelfLife', t('sales.lines.shelfLife'), SHELF_LIFE_MAX)}
          <Field
            name={[line, 'warrantyExpiredDate']}
            label={t('salesDelivery.lines.warrantyExpiry')}
          >
            <FieldInput
              dataType="date"
              placeholder={placeholder('select', t('salesDelivery.lines.warrantyExpiry'))}
            />
          </Field>
          <Field
            name={[line, 'warrantyDescription']}
            label={t('salesDelivery.lines.warrantyText')}
            className="erp-field-span-3"
          >
            <Input
              maxLength={WARRANTY_TEXT_MAX}
              placeholder={placeholder('enter', t('salesDelivery.lines.warrantyText'))}
            />
          </Field>
        </>
      )}
    />
  );
}
