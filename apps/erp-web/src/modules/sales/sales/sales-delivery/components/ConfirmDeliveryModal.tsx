import {
  problemMessage,
  useConfirmSalesDelivery,
  type ConfirmDeliveryRequest,
  type EmployeeOption,
  type SalesDeliveryDetail,
} from '@erp/api-client';
import { selectOptions } from '@erp/metadata-engine';
import {
  API_DATE_FORMAT,
  Field,
  FieldGrid,
  FieldInput,
  formatDate,
  FormModal,
  usePlaceholder,
  type FieldDataType,
} from '@erp/ui';
import { App, Input, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { useRefreshDocuments } from '@/common/documents/documents';
import { trimmed } from '@/common/master-data';
import { CONFIRM_REMARKS_MAX, GRN_MAX } from '../constants';

export function ConfirmDeliveryModal({
  delivery,
  employees,
  open,
  onClose,
}: {
  delivery: SalesDeliveryDetail;
  employees?: EmployeeOption[];
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const placeholder = usePlaceholder();
  const refresh = useRefreshDocuments();
  const confirm = useConfirmSalesDelivery();

  const input = (
    name: keyof ConfirmDeliveryRequest,
    label: string,
    dataType: FieldDataType,
    required = false,
  ) => (
    <Field
      name={name}
      label={label}
      rules={[{ required, message: t('salesDelivery.f.actualDateRequired') }]}
    >
      <FieldInput
        dataType={dataType}
        placeholder={placeholder(dataType === 'date' ? 'select' : 'enter', label)}
      />
    </Field>
  );

  const submit = async (v: Partial<ConfirmDeliveryRequest>) => {
    try {
      await confirm.mutateAsync({
        id: delivery.id,
        data: {
          actualDeliveryDate: v.actualDeliveryDate ?? delivery.deliveryDate,
          collectedById: v.collectedById ?? null,
          grnNo: trimmed(v.grnNo),
          grnDate: v.grnDate ?? null,
          grnAmount: v.grnAmount ?? null,
          grnCollectionDate: v.grnCollectionDate ?? null,
          remarks: trimmed(v.remarks),
        },
      });
      void message.success(t('salesDelivery.confirmDone', { code: delivery.code }));
      await refresh();
      onClose();
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
      throw error;
    }
  };

  return (
    <FormModal<Partial<ConfirmDeliveryRequest>>
      open={open}
      onClose={onClose}
      title={t('salesDelivery.confirmTitle', { code: delivery.code })}
      subtitle={t('salesDelivery.confirmText')}
      submitText={t('salesDelivery.confirm')}
      initialValues={{
        actualDeliveryDate: formatDate(new Date(), API_DATE_FORMAT),
        grnAmount: delivery.netAmount,
      }}
      onSubmit={submit}
    >
      <FieldGrid columns={2}>
        {input('actualDeliveryDate', t('salesDelivery.f.actualDate'), 'date', true)}
        <Field name="collectedById" label={t('salesDelivery.f.collectedBy')}>
          <Select<number>
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder={placeholder('select', t('salesDelivery.f.collectedBy'))}
            options={selectOptions(employees)}
          />
        </Field>
        <Field name="grnNo" label={t('salesDelivery.f.grnNo')}>
          <Input
            maxLength={GRN_MAX}
            placeholder={placeholder('enter', t('salesDelivery.f.grnNo'))}
          />
        </Field>
        {input('grnDate', t('salesDelivery.f.grnDate'), 'date')}
        {input('grnAmount', t('salesDelivery.f.grnAmount'), 'money')}
        {input('grnCollectionDate', t('salesDelivery.f.grnCollectionDate'), 'date')}
      </FieldGrid>
      <Field name="remarks" label={t('salesDelivery.f.remarks')}>
        <Input.TextArea
          rows={2}
          maxLength={CONFIRM_REMARKS_MAX}
          placeholder={placeholder('enter', t('salesDelivery.f.remarks'))}
        />
      </Field>
    </FormModal>
  );
}
