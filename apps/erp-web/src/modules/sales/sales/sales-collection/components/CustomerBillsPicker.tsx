import { getCollectableBills, problemMessage } from '@erp/api-client';
import { selectOptions, type LookupItem } from '@erp/metadata-engine';
import { App, Form, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { fromBill, type CollectionValues } from '../collection-form';

export function CustomerBillsPicker({
  id,
  value,
  onChange,
  customers,
  collectionId,
  placeholder,
}: {
  id?: string;
  value?: number | null;
  onChange?: (value: number | null) => void;
  customers?: LookupItem[];
  collectionId?: number;
  placeholder?: string;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const form = Form.useFormInstance<CollectionValues>();

  const pick = async (next?: number) => {
    onChange?.(next ?? null);
    form.setFieldValue('lines', []);
    if (!next) return;
    try {
      const { bills } = await getCollectableBills({ customerId: next, collectionId });
      if (form.getFieldValue('customerId') === next)
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
      options={selectOptions(customers, value)}
    />
  );
}
