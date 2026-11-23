import { Form, Select } from 'antd';

interface SourceOption {
  id: number;
  code: string;
  supplierName?: string | null;
}

export function SourcePicker<O extends SourceOption, V extends object>({
  id,
  options,
  current,
  placeholder,
  disabled,
  allowClear,
  toValues,
  value,
  onChange,
}: {
  id?: string;
  options?: O[];
  current?: { id: number; name: string };
  placeholder?: string;
  disabled?: boolean;
  allowClear?: boolean;
  toValues: (option: O) => Partial<V>;
  value?: number | null;
  onChange?: (value: number | null) => void;
}) {
  const form = Form.useFormInstance<V>();
  const items = (options ?? []).map((o) => ({
    value: o.id,
    label: o.supplierName ? `${o.code} · ${o.supplierName}` : o.code,
  }));
  if (current && !items.some((o) => o.value === current.id)) {
    items.unshift({ value: current.id, label: current.name });
  }

  const pick = (next?: number) => {
    onChange?.(next ?? null);
    const option = options?.find((o) => o.id === next);
    if (option) form.setFieldsValue(toValues(option) as never);
  };

  return (
    <Select<number>
      id={id}
      showSearch
      allowClear={allowClear}
      optionFilterProp="label"
      placeholder={placeholder}
      disabled={disabled}
      value={value ?? undefined}
      onChange={pick}
      options={items}
    />
  );
}
