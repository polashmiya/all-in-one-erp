import { formatNumber } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { PricedLines } from '../../../components/PricedLines';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { BATCH_MAX } from '../constants';
import { emptyLine, type ReturnLine } from '../return-form';

export function ReturnLines() {
  const { t } = useTranslation();

  return (
    <PricedLines<ReturnLine>
      fixed
      headerCharges={false}
      description={t('return.lines.description')}
      newLine={emptyLine}
      onPick={() => ({})}
      note={{
        name: 'batch',
        placeholder: t('return.lines.batchPlaceholder'),
        maxLength: BATCH_MAX,
      }}
      columns={[
        {
          key: 'remaining',
          title: t('return.lines.left'),
          className: 'erp-lines__amount',
          align: 'end',
          render: (_, line) =>
            line?.remaining == null ? '—' : formatNumber(line.remaining, QUANTITY_DECIMALS_SHOWN),
        },
      ]}
    />
  );
}
