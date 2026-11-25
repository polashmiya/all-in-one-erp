import type { LookupItem } from '@erp/metadata-engine';
import { Form, Select } from 'antd';

export function TypeSelect<V extends object>({
  id,
  types,
  placeholder,
  toValues,
  value,
  onChange,
}: {
  id?: string;
  types: LookupItem[];
  placeholder?: string;
  toValues: (type: string) => Partial<V>;
  value?: string;
  onChange?: (value: string) => void;
}) {
  const form = Form.useFormInstance<V>();

  return (
    <Select<string>
      id={id}
      value={value}
      placeholder={placeholder}
      options={types.map((type) => ({ value: String(type.id), label: type.name }))}
      onChange={(next) => {
        onChange?.(next);
        form.setFieldsValue(toValues(next) as never);
      }}
    />
  );
}
