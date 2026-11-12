import {
  useCreateItemCategory,
  useUpdateItemCategory,
  type FeatureConfig,
  type ItemCategoryRow,
  type ItemLookup,
  type SaveItemCategoryRequest,
} from '@erp/api-client';
import { Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { MasterFormModal, trimmed } from '@/common/master-data';
import { ITEM_LOOKUPS } from '../../item-management';

export function CategoryFormModal({
  open,
  category,
  config,
  defaultDepartmentId,
  departments,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  category?: ItemCategoryRow;
  config: FeatureConfig;
  defaultDepartmentId?: number;
  departments?: ItemLookup[];
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SaveItemCategoryRequest>();
  const create = useCreateItemCategory();
  const update = useUpdateItemCategory();
  const initialValues = useMemo(
    () => category ?? { departmentId: defaultDepartmentId },
    [category, defaultDepartmentId],
  );

  const save = async (values: SaveItemCategoryRequest) => {
    const data: SaveItemCategoryRequest = {
      departmentId: values.departmentId,
      code: values.code.trim().toUpperCase(),
      name: values.name.trim(),
      imageSource: trimmed(values.imageSource),
      isActive: values.isActive ?? true,
      customData: values.customData ?? null,
    };
    const saved = category
      ? await update.mutateAsync({ id: category.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<SaveItemCategoryRequest>
      open={open}
      onClose={onClose}
      title={category ? t('cat.form.editTitle') : t('cat.form.newTitle')}
      subtitle={t('cat.form.subtitle', { unit: unitName })}
      form={form}
      config={config}
      options={{ lookups: { [ITEM_LOOKUPS.department]: departments } }}
      initialValues={initialValues}
      editing={!!category}
      onSave={save}
    />
  );
}
