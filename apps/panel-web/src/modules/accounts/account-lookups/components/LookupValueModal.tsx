import {
  ApiError,
  useCreateLookupValue,
  useUpdateLookupValue,
  type LookupValueRow,
  type SaveLookupValueRequest,
} from '@erp/api-client';
import { Field, FieldGrid, FormModal, YesNoSwitch } from '@erp/ui';
import { App, Form, Input, InputNumber } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';

const CODE_MAX = 50;
const NAME_MAX = 200;
const ORDER_STEP = 10;

export function LookupValueModal({
  accountId,
  type,
  listName,
  open,
  value,
  nextOrder,
  onClose,
  onSaved,
}: {
  accountId: string;
  type: string;
  listName: string;
  open: boolean;
  value?: LookupValueRow;
  nextOrder: number;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [form] = Form.useForm<SaveLookupValueRequest>();
  const create = useCreateLookupValue();
  const update = useUpdateLookupValue();

  const initialValues = useMemo<Partial<SaveLookupValueRequest>>(
    () =>
      value ?? {
        sortOrder: nextOrder + ORDER_STEP,
        isDefault: false,
        isActive: true,
      },
    [value, nextOrder],
  );

  const submit = async (data: SaveLookupValueRequest) => {
    try {
      if (value) await update.mutateAsync({ id: accountId, type, valueId: value.id, data });
      else await create.mutateAsync({ id: accountId, type, data });
      await onSaved();
      void message.success(t('lookups.saved'));
    } catch (error) {
      if (error instanceof ApiError && error.status === 400)
        form.setFields(
          Object.entries(error.fieldErrors).map(([name, errors]) => ({
            name: name as keyof SaveLookupValueRequest,
            errors,
          })),
        );
      void message.error(
        error instanceof ApiError
          ? (Object.values(error.fieldErrors)[0]?.[0] ?? error.message)
          : t('lookups.failed'),
      );
      throw error;
    }
  };

  return (
    <FormModal<SaveLookupValueRequest>
      open={open}
      onClose={onClose}
      form={form}
      title={t(value ? 'lookups.editTitle' : 'lookups.newTitle', { list: listName })}
      initialValues={initialValues}
      addAnotherText={value ? undefined : t('lookups.addAnother')}
      onSubmit={submit}
    >
      <FieldGrid columns={2}>
        <Field
          name="nameEn"
          label={t('lookups.f.nameEn')}
          rules={[{ required: true, whitespace: true, message: t('lookups.f.nameRequired') }]}
        >
          <Input maxLength={NAME_MAX} />
        </Field>
        <Field name="nameBn" label={t('lookups.f.nameBn')}>
          <Input maxLength={NAME_MAX} />
        </Field>
        <Field name="code" label={t('lookups.f.code')}>
          <Input maxLength={CODE_MAX} />
        </Field>
        <Field name="sortOrder" label={t('lookups.f.order')}>
          <InputNumber min={0} precision={0} className="erp-w-full" />
        </Field>
        <Field
          name="isDefault"
          label={t('lookups.f.default')}
          valuePropName="checked"
          extra={t('lookups.f.defaultHint')}
        >
          <YesNoSwitch />
        </Field>
        <Field name="isActive" label={t('lookups.f.active')} valuePropName="checked">
          <YesNoSwitch />
        </Field>
      </FieldGrid>
    </FormModal>
  );
}
