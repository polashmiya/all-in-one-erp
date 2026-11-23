import { useTranslation } from 'react-i18next';
import { PricedLines } from '../../../components/PricedLines';
import { SPECIFICATION_MAX } from '../constants';
import { emptyLine, type OrderLine } from '../order-form';

export function OrderLines() {
  const { t } = useTranslation();

  return (
    <PricedLines<OrderLine>
      description={t('order.lines.description')}
      newLine={emptyLine}
      onPick={(option) => ({ price: option.rate, vatPercentage: option.vatPercentage })}
      note={{
        name: 'specification',
        placeholder: t('order.lines.specificationPlaceholder'),
        maxLength: SPECIFICATION_MAX,
      }}
    />
  );
}
