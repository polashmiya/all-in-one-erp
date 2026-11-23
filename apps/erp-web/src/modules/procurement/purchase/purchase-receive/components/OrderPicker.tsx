import type { ReceiveOrderOption } from '@erp/api-client';
import { Form, Select } from 'antd';
import { fromOrder, type ReceiveValues } from '../receive-form';

export function OrderPicker({
  id,
  options,
  current,
  placeholder,
  disabled,
  value,
  onChange,
}: {
  id?: string;
  options?: ReceiveOrderOption[];
  current?: { id: number; name: string };
  placeholder?: string;
  disabled?: boolean;
  value?: number | null;
  onChange?: (value: number | null) => void;
}) {
  const form = Form.useFormInstance<ReceiveValues>();
  const items = (options ?? []).map((o) => ({
    value: o.id,
    label: o.supplierName ? `${o.code} · ${o.supplierName}` : o.code,
  }));
  if (current && !items.some((o) => o.value === current.id)) {
    items.unshift({ value: current.id, label: current.name });
  }

  const pick = (next?: number) => {
    onChange?.(next ?? null);
    const order = options?.find((o) => o.id === next);
    if (order) form.setFieldsValue(fromOrder(order));
  };

  return (
    <Select<number>
      id={id}
      showSearch
      optionFilterProp="label"
      placeholder={placeholder}
      disabled={disabled}
      value={value ?? undefined}
      onChange={pick}
      options={items}
    />
  );
}
