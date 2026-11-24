import { formatNumber } from '@erp/ui';
import { Form } from 'antd';
import { useTranslation } from 'react-i18next';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { LineGrid } from '@/common/documents/LineGrid';
import { useOnHand } from '../../../inventory';
import { LINE_REMARKS_MAX } from '../constants';
import { emptyLine, type TransferLine } from '../transfer-form';

export function TransferLines() {
  const { t } = useTranslation();
  const form = Form.useFormInstance();
  const warehouseId: string | undefined = Form.useWatch('fromWarehouseId', form);
  const lines: TransferLine[] = Form.useWatch('lines', form) ?? [];
  const onHand = useOnHand(
    warehouseId,
    lines.flatMap((line) => line?.itemId ?? []),
  );

  return (
    <LineGrid<TransferLine>
      description={t('transfer.lines.description')}
      newLine={emptyLine}
      onPick={() => ({})}
      note={{
        name: 'remarks',
        placeholder: t('transfer.lines.remarksPlaceholder'),
        maxLength: LINE_REMARKS_MAX,
      }}
      columns={[
        {
          key: 'onHand',
          title: t('transfer.lines.onHand'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (_, line) => {
            const stock = onHand(line?.itemId);
            return stock ? formatNumber(stock.quantity, QUANTITY_DECIMALS_SHOWN) : '—';
          },
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
          strong: true,
        },
      ]}
    />
  );
}
