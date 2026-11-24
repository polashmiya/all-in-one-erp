import { getPendingTaxes, problemMessage } from '@erp/api-client';
import { selectOptions, type LookupItem } from '@erp/metadata-engine';
import { App, Form, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { fromTax, type DepositValues } from '../deposit-form';

export function TaxTypePicker({
  id,
  value,
  onChange,
  types,
  depositId,
  placeholder,
}: {
  id?: string;
  value?: string | null;
  onChange?: (value: string) => void;
  types: LookupItem[];
  depositId?: number;
  placeholder?: string;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const form = Form.useFormInstance<DepositValues>();

  const pick = async (next: string) => {
    onChange?.(next);
    form.setFieldValue('lines', []);
    try {
      const taxes = await getPendingTaxes({ type: next, depositId });
      if (form.getFieldValue('type') === next) form.setFieldValue('lines', taxes.map(fromTax));
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  return (
    <Select<string>
      id={id}
      placeholder={placeholder}
      value={value ?? undefined}
      onChange={(next) => void pick(next)}
      options={selectOptions(types, value) as { value: string; label: string }[]}
    />
  );
}
