import type { ItemLookup } from '@erp/api-client';
import { UnitInput } from '@erp/ui';
import { Form } from 'antd';

export function ConversionInput({
  uoms,
  id,
  placeholder,
  value,
  onChange,
}: {
  uoms?: ItemLookup[];
  id?: string;
  placeholder?: string;
  value?: number | null;
  onChange?: (value: number | null) => void;
}) {
  const uomId = Form.useWatch('uomId');
  const unit = uoms?.find((u) => u.id === uomId)?.code ?? '';

  return (
    <UnitInput
      id={id}
      unit={unit}
      min={0.0001}
      precision={4}
      placeholder={placeholder}
      value={value}
      onChange={onChange}
      className="erp-w-full"
    />
  );
}
