import type { CustomerOption, ReturnDeliveryOption } from '@erp/api-client';
import { Form } from 'antd';
import { SourcePicker } from '@/common/documents/SourcePicker';
import { CustomerPicker } from '../../../components/CustomerPicker';
import { WITHOUT_REFERENCE } from '../constants';
import { fromDelivery, fromReturnCustomer, type ReturnValues } from '../return-form';

interface PickerProps {
  current?: { id: number; name: string };
  placeholder?: string;
}

function useWithoutReference() {
  return Form.useWatch('source', Form.useFormInstance()) === WITHOUT_REFERENCE;
}

export function ReturnDeliveryPicker({
  deliveries,
  saved,
  ...props
}: PickerProps & { deliveries?: ReturnDeliveryOption[]; saved: boolean }) {
  const free = useWithoutReference();
  return (
    <SourcePicker<ReturnDeliveryOption, ReturnValues>
      {...props}
      disabled={saved || free}
      toValues={fromDelivery}
      labelOf={(option) => `${option.code} · ${option.customerName}`}
      options={deliveries}
    />
  );
}

export function ReturnCustomerPicker({
  customers,
  ...props
}: PickerProps & { customers?: CustomerOption[] }) {
  const free = useWithoutReference();
  return (
    <CustomerPicker<ReturnValues>
      {...props}
      disabled={!free}
      toValues={fromReturnCustomer}
      customers={customers}
    />
  );
}
