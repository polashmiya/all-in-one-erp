import { Field, formatMoney, formatNumber, MoneyInput, usePlaceholder, YesNoSwitch } from '@erp/ui';
import { Form, Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import type { PayFrom } from '@/common/pay-from';
import { SalesLines } from '../../../components/SalesLines';
import { emptyLine } from '../../../sales-document';
import { BATCH_MAX, REMARKS_MAX, SERIAL_MAX, WITHOUT_REFERENCE } from '../constants';
import { returnTotals, type ReturnLine, type ReturnSource } from '../return-form';

const HALF = 'erp-field-span-3';
const WIDE = 'erp-field-span-4';

export function ReturnLines({ payFrom }: { payFrom: PayFrom }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const free = Form.useWatch('source', form) === WITHOUT_REFERENCE;
  const lines: ReturnLine[] = Form.useWatch('lines', form) ?? [];
  const source: ReturnSource = {
    isVatInclusive: Form.useWatch('isVatInclusive', { form, preserve: true }),
    due: Form.useWatch('due', { form, preserve: true }),
    reductionRate: Form.useWatch('reductionRate', { form, preserve: true }),
  };
  const charge: number = Form.useWatch('chargeRefundAmount', form) ?? 0;
  const deduction: number = Form.useWatch('deductionValue', form) ?? 0;
  const refund: number = Form.useWatch('refundAmount', form) ?? 0;
  const totals = returnTotals(lines, source, { charge, deduction, refund });
  payFrom.useClear();

  const text = (
    line: number,
    key: keyof ReturnLine,
    label: string,
    maxLength: number,
    className?: string,
  ) => (
    <Field name={[line, key]} label={label} className={className}>
      <Input maxLength={maxLength} placeholder={placeholder('enter', label)} />
    </Field>
  );
  const money = (key: string, label: string, value: number) => ({
    key,
    label,
    value: formatMoney(value),
  });

  return (
    <SalesLines<ReturnLine>
      locked={!free}
      warranty={false}
      description={t(free ? 'salesReturn.lines.freeDescription' : 'salesReturn.lines.description')}
      newLine={emptyLine}
      header={{
        isVatInclusive: source.isVatInclusive,
        charges: charge,
        reductions: totals.reductions,
      }}
      columns={
        free
          ? []
          : [
              {
                key: 'remaining',
                title: t('salesReturn.lines.remaining'),
                className: 'erp-lines__amount',
                align: 'end',
                render: (_, line) => formatNumber(line?.remaining, QUANTITY_DECIMALS_SHOWN),
              },
            ]
      }
      fields={(line) => (
        <>
          {free && (
            <Field name={[line, 'costPrice']} label={t('salesReturn.lines.costPrice')}>
              <MoneyInput
                compact
                className="erp-w-full"
                placeholder={t('salesReturn.lines.costPlaceholder')}
              />
            </Field>
          )}
          <Field
            name={[line, 'isDamaged']}
            label={t('salesReturn.lines.damaged')}
            valuePropName="checked"
          >
            <YesNoSwitch />
          </Field>
          {text(line, 'batch', t('salesReturn.lines.batch'), BATCH_MAX)}
          {text(
            line,
            'itemSerial',
            t('salesReturn.lines.serial'),
            SERIAL_MAX,
            free ? HALF : undefined,
          )}
          {text(line, 'remarks', t('salesReturn.lines.remarks'), REMARKS_MAX, free ? HALF : WIDE)}
        </>
      )}
      settlement={[
        ...(free ? [] : [money('due', t('salesReturn.dueAdjusted'), totals.dueAdjusted)]),
        money('refund', t('salesReturn.refund'), refund),
        money('advance', t('salesReturn.advance'), totals.advance),
      ]}
    />
  );
}
