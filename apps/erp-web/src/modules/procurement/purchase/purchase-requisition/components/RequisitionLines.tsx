import { formatMoney, formatNumber, MoneyInput, usePlaceholder } from '@erp/ui';
import { Form, Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { LineGrid } from '../../../components/LineGrid';
import { QUANTITY_DECIMALS_SHOWN } from '../../../procurement';
import { LINE_REMARKS_MAX } from '../constants';
import { emptyLine, lineAmount, type RequisitionLineValue } from '../requisition-form';

type Line = Partial<RequisitionLineValue>;

export function RequisitionLines() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();

  return (
    <LineGrid<Line>
      description={t('requisition.lines.description')}
      newLine={emptyLine}
      onPick={(option) => ({ rate: option.rate })}
      columns={[
        {
          key: 'rate',
          title: t('requisition.lines.rate'),
          className: 'erp-lines__money',
          align: 'end',
          render: (field) => (
            <Form.Item name={[field.name, 'rate']}>
              <MoneyInput
                className="erp-w-full"
                placeholder={placeholder('enter', t('requisition.lines.rate'))}
              />
            </Form.Item>
          ),
        },
        {
          key: 'amount',
          title: t('requisition.lines.amount'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (_, line) => formatMoney(lineAmount(line)),
        },
        {
          key: 'remarks',
          title: t('requisition.lines.remarks'),
          className: 'erp-lines__text',
          render: (field) => (
            <Form.Item name={[field.name, 'remarks']}>
              <Input
                maxLength={LINE_REMARKS_MAX}
                placeholder={t('requisition.lines.remarksPlaceholder')}
              />
            </Form.Item>
          ),
        },
      ]}
      totals={(lines) => [
        {
          key: 'quantity',
          label: t('procurement.lines.totalQuantity'),
          value: formatNumber(
            lines.reduce((sum, line) => sum + (line?.quantity ?? 0), 0),
            QUANTITY_DECIMALS_SHOWN,
          ),
        },
        {
          key: 'amount',
          label: t('requisition.col.amount'),
          value: formatMoney(lines.reduce((sum, line) => sum + lineAmount(line), 0)),
          strong: true,
        },
      ]}
    />
  );
}
