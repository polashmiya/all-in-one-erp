import type { CustomerOption } from '@erp/api-client';
import { SourcePicker } from '@/common/documents/SourcePicker';
import { lookupLabel } from '@/common/master-data';

export function CustomerPicker<V extends object>({
  customers,
  ...props
}: {
  id?: string;
  customers?: CustomerOption[];
  current?: { id: number; name: string };
  placeholder?: string;
  disabled?: boolean;
  toValues: (customer: CustomerOption) => Partial<V>;
  value?: number | null;
  onChange?: (value: number | null) => void;
}) {
  return (
    <SourcePicker<CustomerOption, V>
      {...props}
      labelOf={lookupLabel}
      options={customers?.filter((customer) => customer.isActive)}
    />
  );
}
