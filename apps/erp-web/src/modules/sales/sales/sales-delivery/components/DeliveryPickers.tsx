import type { CustomerOption, DeliveryOrderOption } from '@erp/api-client';
import { Form } from 'antd';
import { SourcePicker } from '@/common/documents/SourcePicker';
import { CustomerPicker } from '../../../components/CustomerPicker';
import { FROM_ORDER } from '../constants';
import { fromDeliveryCustomer, fromOrder, type DeliveryValues } from '../delivery-form';

interface PickerProps {
  current?: { id: number; name: string };
  placeholder?: string;
}

function useFromOrder() {
  return Form.useWatch('type', Form.useFormInstance()) === FROM_ORDER;
}

export function OrderPicker({
  orders,
  ...props
}: PickerProps & { orders?: DeliveryOrderOption[] }) {
  const ordered = useFromOrder();
  return (
    <SourcePicker<DeliveryOrderOption, DeliveryValues>
      {...props}
      disabled={!ordered}
      toValues={fromOrder}
      labelOf={(option) => `${option.code} · ${option.customerName}`}
      options={orders}
    />
  );
}

export function DeliveryCustomerPicker({
  customers,
  ...props
}: PickerProps & { customers?: CustomerOption[] }) {
  const ordered = useFromOrder();
  return (
    <CustomerPicker<DeliveryValues>
      {...props}
      disabled={ordered}
      toValues={fromDeliveryCustomer}
      customers={customers}
    />
  );
}
