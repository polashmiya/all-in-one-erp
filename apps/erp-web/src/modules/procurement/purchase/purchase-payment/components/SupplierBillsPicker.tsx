import { getPayableBills, problemMessage } from '@erp/api-client';
import { selectOptions, type LookupItem } from '@erp/metadata-engine';
import { App, Form, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { fromBill, type PaymentValues } from '../payment-form';

export function SupplierBillsPicker({
  id,
  value,
  onChange,
  suppliers,
  paymentId,
  placeholder,
}: {
  id?: string;
  value?: number | null;
  onChange?: (value: number | null) => void;
  suppliers?: LookupItem[];
  paymentId?: number;
  placeholder?: string;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const form = Form.useFormInstance<PaymentValues>();

  const pick = async (next?: number) => {
    onChange?.(next ?? null);
    form.setFieldValue('lines', []);
    if (!next) return;
    try {
      const { bills } = await getPayableBills({ supplierId: next, paymentId });
      if (form.getFieldValue('supplierId') === next)
        form.setFieldValue('lines', bills.map(fromBill));
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  return (
    <Select<number>
      id={id}
      showSearch
      optionFilterProp="label"
      placeholder={placeholder}
      value={value ?? undefined}
      onChange={(next) => void pick(next)}
      options={selectOptions(suppliers, value)}
    />
  );
}
