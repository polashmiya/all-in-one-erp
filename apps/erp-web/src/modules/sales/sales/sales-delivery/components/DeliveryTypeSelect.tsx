import type { LookupItem } from '@erp/metadata-engine';
import { Form, Select } from 'antd';
import { ofType, type DeliveryValues } from '../delivery-form';

export function DeliveryTypeSelect({
  id,
  types,
  placeholder,
  value,
  onChange,
}: {
  id?: string;
  types: LookupItem[];
  placeholder?: string;
  value?: string;
  onChange?: (value: string) => void;
}) {
  const form = Form.useFormInstance<DeliveryValues>();

  return (
    <Select<string>
      id={id}
      value={value}
      placeholder={placeholder}
      options={types.map((type) => ({ value: String(type.id), label: type.name }))}
      onChange={(next) => {
        onChange?.(next);
        form.setFieldsValue(ofType(next) as never);
      }}
    />
  );
}
