import {
  useCreateItemDepartment,
  useUpdateItemDepartment,
  type ItemDepartmentRow,
  type SaveItemDepartmentRequest,
} from '@erp/api-client';
import { Field, FieldGrid } from '@erp/ui';
import { Form, Input, InputNumber } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActiveField,
  ImageField,
  MasterFormModal,
  CODE_PATTERN,
  toUpper,
} from '@/common/master-data';

interface DepartmentFormValues {
  code: string;
  name: string;
  serial?: number | null;
  imageSource?: string | null;
  isActive: boolean;
}

const CODE_MAX = 20;
const NAME_MAX = 100;

export function DepartmentFormModal({
  open,
  department,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  department?: ItemDepartmentRow;
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<DepartmentFormValues>();
  const create = useCreateItemDepartment();
  const update = useUpdateItemDepartment();

  const initialValues = useMemo<Partial<DepartmentFormValues>>(
    () =>
      department
        ? {
            code: department.code,
            name: department.name,
            serial: department.serial,
            imageSource: department.imageSource,
            isActive: department.isActive,
          }
        : { isActive: true },
    [department],
  );

  const save = async (values: DepartmentFormValues) => {
    const data: SaveItemDepartmentRequest = {
      code: values.code.trim(),
      name: values.name.trim(),
      serial: values.serial ?? null,
      imageSource: values.imageSource ?? null,
      isActive: values.isActive,
    };
    const saved = department
      ? await update.mutateAsync({ id: department.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<DepartmentFormValues>
      open={open}
      onClose={onClose}
      title={department ? t('dept.form.editTitle') : t('dept.form.newTitle')}
      subtitle={t('dept.form.subtitle', { unit: unitName })}
      form={form}
      initialValues={initialValues}
      editing={!!department}
      onSave={save}
    >
      <FieldGrid columns={2}>
        <Field
          name="code"
          label={t('dept.f.code')}
          normalize={toUpper}
          rules={[
            { required: true, whitespace: true, message: t('dept.f.codeRequired') },
            { max: CODE_MAX, message: t('master.f.maxLength', { count: CODE_MAX }) },
            { pattern: CODE_PATTERN, message: t('master.f.codeInvalid') },
          ]}
        >
          <Input maxLength={CODE_MAX} placeholder={t('dept.f.codePlaceholder')} autoFocus />
        </Field>
        <Field
          name="serial"
          label={t('dept.f.serial')}
          extra={department ? t('dept.f.serialHint') : t('dept.f.serialNewHint')}
        >
          <InputNumber<number> min={0} max={999_999} precision={0} className="erp-w-full" />
        </Field>
      </FieldGrid>
      <Field
        name="name"
        label={t('dept.f.name')}
        rules={[
          { required: true, whitespace: true, message: t('dept.f.nameRequired') },
          { max: NAME_MAX, message: t('master.f.maxLength', { count: NAME_MAX }) },
        ]}
      >
        <Input maxLength={NAME_MAX} placeholder={t('dept.f.namePlaceholder')} />
      </Field>
      <ImageField />
      <ActiveField />
    </MasterFormModal>
  );
}
