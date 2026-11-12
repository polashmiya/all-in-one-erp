import {
  useCreateItemDepartment,
  useUpdateItemDepartment,
  type FeatureConfig,
  type ItemDepartmentRow,
  type SaveItemDepartmentRequest,
} from '@erp/api-client';
import { Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { MasterFormModal, trimmed } from '@/common/master-data';

export function DepartmentFormModal({
  open,
  department,
  config,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  department?: ItemDepartmentRow;
  config: FeatureConfig;
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SaveItemDepartmentRequest>();
  const create = useCreateItemDepartment();
  const update = useUpdateItemDepartment();
  const initialValues = useMemo(() => department ?? {}, [department]);

  const save = async (values: SaveItemDepartmentRequest) => {
    const data: SaveItemDepartmentRequest = {
      code: values.code.trim().toUpperCase(),
      name: values.name.trim(),
      serial: values.serial ?? null,
      imageSource: trimmed(values.imageSource),
      isActive: values.isActive ?? true,
      customData: values.customData ?? null,
    };
    const saved = department
      ? await update.mutateAsync({ id: department.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<SaveItemDepartmentRequest>
      open={open}
      onClose={onClose}
      title={department ? t('dept.form.editTitle') : t('dept.form.newTitle')}
      subtitle={t('dept.form.subtitle', { unit: unitName })}
      form={form}
      config={config}
      initialValues={initialValues}
      editing={!!department}
      onSave={save}
    />
  );
}
