import { Form, Select } from 'antd';

interface SourceOption {
  id: number;
  code: string;
  supplierName?: string | null;
}

const sourceLabel = (option: SourceOption) =>
  option.supplierName ? `${option.code} · ${option.supplierName}` : option.code;

export function SourcePicker<O extends SourceOption, V extends object>({
  id,
  options,
  current,
  placeholder,
  disabled,
  allowClear,
  toValues,
  labelOf = sourceLabel,
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
  labelOf?: (option: O) => string;
  value?: number | null;
  onChange?: (value: number | null) => void;
}) {
  const form = Form.useFormInstance<V>();
  const items = (options ?? []).map((o) => ({
    value: o.id,
    label: labelOf(o),
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
