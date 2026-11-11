import { useCreateUom, useUpdateUom, type SaveUomRequest, type UomRow } from '@erp/api-client';
import { Field, FieldGrid } from '@erp/ui';
import { Form, Input, InputNumber, Switch } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { ActiveField, MasterFormModal, CODE_PATTERN, trimmed } from '@/common/master-data';

interface UomFormValues {
  code: string;
  name: string;
  description?: string | null;
  isDecimalAllowed: boolean;
  roundingLength?: number | null;
  isActive: boolean;
}

const CODE_MAX = 10;
const NAME_MAX = 50;
const DESCRIPTION_MAX = 255;
const MAX_DECIMALS = 6;
const DEFAULT_DECIMALS = 2;

export function UomFormModal({
  open,
  uom,
  accountName,
  onClose,
  onSaved,
}: {
  open: boolean;
  uom?: UomRow;
  accountName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<UomFormValues>();
  const decimalAllowed = Form.useWatch('isDecimalAllowed', form);
  const create = useCreateUom();
  const update = useUpdateUom();

  const initialValues = useMemo<Partial<UomFormValues>>(
    () =>
      uom
        ? {
            code: uom.code,
            name: uom.name,
            description: uom.description,
            isDecimalAllowed: uom.isDecimalAllowed,
            roundingLength: uom.isDecimalAllowed ? uom.roundingLength : DEFAULT_DECIMALS,
            isActive: uom.isActive,
          }
        : { isDecimalAllowed: false, roundingLength: DEFAULT_DECIMALS, isActive: true },
    [uom],
  );

  const save = async (values: UomFormValues) => {
    const data: SaveUomRequest = {
      code: values.code.trim(),
      name: values.name.trim(),
      description: trimmed(values.description),
      isDecimalAllowed: values.isDecimalAllowed,
      roundingLength: values.isDecimalAllowed ? (values.roundingLength ?? null) : null,
      isActive: values.isActive,
    };
    const saved = uom
      ? await update.mutateAsync({ id: uom.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<UomFormValues>
      open={open}
      onClose={onClose}
      title={uom ? t('uom.form.editTitle') : t('uom.form.newTitle')}
      subtitle={t('uom.form.subtitle', { account: accountName })}
      form={form}
      initialValues={initialValues}
      editing={!!uom}
      onSave={save}
    >
      <FieldGrid columns={2}>
        <Field
          name="code"
          label={t('uom.f.code')}
          rules={[
            { required: true, whitespace: true, message: t('uom.f.codeRequired') },
            { max: CODE_MAX, message: t('master.f.maxLength', { count: CODE_MAX }) },
            { pattern: CODE_PATTERN, message: t('master.f.codeInvalid') },
          ]}
        >
          <Input maxLength={CODE_MAX} placeholder={t('uom.f.codePlaceholder')} autoFocus />
        </Field>
        <Field
          name="name"
          label={t('uom.f.name')}
          rules={[
            { required: true, whitespace: true, message: t('uom.f.nameRequired') },
            { max: NAME_MAX, message: t('master.f.maxLength', { count: NAME_MAX }) },
          ]}
        >
          <Input maxLength={NAME_MAX} placeholder={t('uom.f.namePlaceholder')} />
        </Field>
      </FieldGrid>
      <Field
        name="description"
        label={t('uom.f.description')}
        rules={[
          { max: DESCRIPTION_MAX, message: t('master.f.maxLength', { count: DESCRIPTION_MAX }) },
        ]}
      >
        <Input.TextArea
          maxLength={DESCRIPTION_MAX}
          showCount
          autoSize={{ minRows: 2, maxRows: 4 }}
          placeholder={t('uom.f.descriptionPlaceholder')}
        />
      </Field>
      <FieldGrid columns={2}>
        <Field
          name="isDecimalAllowed"
          label={t('uom.f.decimalAllowed')}
          valuePropName="checked"
          extra={t('uom.f.decimalAllowedHint')}
        >
          <Switch />
        </Field>
        {decimalAllowed && (
          <Field
            name="roundingLength"
            label={t('uom.f.roundingLength')}
            extra={t('uom.f.roundingLengthHint')}
            rules={[{ required: true, message: t('uom.f.roundingLengthRequired') }]}
          >
            <InputNumber<number> min={1} max={MAX_DECIMALS} precision={0} className="erp-w-full" />
          </Field>
        )}
      </FieldGrid>
      <ActiveField />
    </MasterFormModal>
  );
}
