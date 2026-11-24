import { formatMoney, formatNumber, MoneyInput, usePlaceholder } from '@erp/ui';
import { Form } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { LineGrid, type LineColumn } from '@/common/documents/LineGrid';
import { useOnHand } from '../../../inventory';
import { emptyLine, lineAmount, type AdjustmentLine } from '../adjustment-form';
import { LINE_REMARKS_MAX, STOCK_OUT } from '../constants';

export function AdjustmentLines() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const out = Form.useWatch('type', form) === STOCK_OUT;
  const warehouseId: string | undefined = Form.useWatch('warehouseId', form);
  const lines: AdjustmentLine[] = Form.useWatch('lines', form) ?? [];
  const onHand = useOnHand(
    warehouseId,
    lines.flatMap((line) => line?.itemId ?? []),
  );
  const costOf = (line?: AdjustmentLine) => (out ? (onHand(line?.itemId)?.cost ?? 0) : undefined);

  const rate: LineColumn<AdjustmentLine> = {
    key: 'rate',
    title: t('adjustment.lines.rate'),
    className: 'erp-lines__money',
    align: 'end',
    render: (field, line) =>
      out ? (
        formatMoney(costOf(line))
      ) : (
        <Form.Item name={[field.name, 'rate']}>
          <MoneyInput
            compact
            className="erp-w-full"
            placeholder={placeholder('enter', t('adjustment.lines.rate'))}
          />
        </Form.Item>
      ),
  };

  return (
    <LineGrid<AdjustmentLine>
      description={t(out ? 'adjustment.lines.outDescription' : 'adjustment.lines.inDescription')}
      newLine={emptyLine}
      onPick={(option) => ({ rate: option.rate })}
      note={{
        name: 'remarks',
        placeholder: t('adjustment.lines.remarksPlaceholder'),
        maxLength: LINE_REMARKS_MAX,
      }}
      columns={[
        {
          key: 'onHand',
          title: t('adjustment.lines.onHand'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (_, line) => {
            const stock = onHand(line?.itemId);
            return stock ? formatNumber(stock.quantity, QUANTITY_DECIMALS_SHOWN) : '—';
          },
        },
        rate,
        {
          key: 'amount',
          title: t('adjustment.lines.amount'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (_, line) => formatMoney(lineAmount(line, costOf(line))),
        },
      ]}
      totals={(all) => [
        {
          key: 'quantity',
          label: t('document.lines.totalQuantity'),
          value: formatNumber(
            all.reduce((sum, line) => sum + (line?.quantity ?? 0), 0),
            QUANTITY_DECIMALS_SHOWN,
          ),
        },
        {
          key: 'amount',
          label: t('adjustment.totals.amount'),
          value: formatMoney(all.reduce((sum, line) => sum + lineAmount(line, costOf(line)), 0)),
          strong: true,
        },
      ]}
    />
  );
}
