import type { OrderRequisitionOption } from '@erp/api-client';
import { Form, Select } from 'antd';
import type { OrderValues } from '../order-form';

export function RequisitionPicker({
  id,
  options,
  current,
  placeholder,
  value,
  onChange,
}: {
  id?: string;
  options?: OrderRequisitionOption[];
  current?: { id: number; name: string };
  placeholder?: string;
  value?: number | null;
  onChange?: (value: number | null) => void;
}) {
  const form = Form.useFormInstance<OrderValues>();
  const items = (options ?? []).map((o) => ({ value: o.id, label: o.code }));
  if (current && !items.some((o) => o.value === current.id)) {
    items.unshift({ value: current.id, label: current.name });
  }

  const pick = (next?: number) => {
    onChange?.(next ?? null);
    const requisition = options?.find((o) => o.id === next);
    if (!requisition) return;
    form.setFieldsValue({
      warehouseId: requisition.warehouseId,
      lines: requisition.lines.map((line) => ({
        itemId: line.itemId,
        itemCode: line.itemCode,
        itemName: line.itemName,
        uomName: line.uomName,
        quantity: line.quantity,
        price: line.rate,
        discount: null,
        vatPercentage: line.vatPercentage,
      })),
    });
  };

  return (
    <Select<number>
      id={id}
      showSearch
      allowClear
      optionFilterProp="label"
      placeholder={placeholder}
      value={value ?? undefined}
      onChange={pick}
      options={items}
    />
  );
}
