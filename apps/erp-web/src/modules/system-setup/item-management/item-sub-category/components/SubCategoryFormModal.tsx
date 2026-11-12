import {
  useCreateItemSubCategory,
  useUpdateItemSubCategory,
  type FeatureConfig,
  type ItemLookup,
  type ItemSubCategoryRow,
  type SaveItemSubCategoryRequest,
} from '@erp/api-client';
import { Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { MasterFormModal, trimmed } from '@/common/master-data';
import { ITEM_LOOKUPS } from '../../item-management';

type SubCategoryFormValues = SaveItemSubCategoryRequest & { departmentId?: number | null };

export function SubCategoryFormModal({
  open,
  subCategory,
  config,
  defaults,
  departments,
  categories,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  subCategory?: ItemSubCategoryRow;
  config: FeatureConfig;
  defaults: { departmentId?: number; categoryId?: number };
  departments?: ItemLookup[];
  categories?: ItemLookup[];
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SubCategoryFormValues>();
  const create = useCreateItemSubCategory();
  const update = useUpdateItemSubCategory();
  const initialValues = useMemo<Partial<SubCategoryFormValues>>(
    () => subCategory ?? { departmentId: defaults.departmentId, categoryId: defaults.categoryId },
    [subCategory, defaults.departmentId, defaults.categoryId],
  );

  const save = async (values: SubCategoryFormValues) => {
    const data: SaveItemSubCategoryRequest = {
      categoryId: values.categoryId,
      name: values.name.trim(),
      imageSource: trimmed(values.imageSource),
      isActive: values.isActive ?? true,
      customData: values.customData ?? null,
    };
    const saved = subCategory
      ? await update.mutateAsync({ id: subCategory.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<SubCategoryFormValues>
      open={open}
      onClose={onClose}
      title={subCategory ? t('subcat.form.editTitle') : t('subcat.form.newTitle')}
      subtitle={t('subcat.form.subtitle', { unit: unitName })}
      form={form}
      config={config}
      options={{
        lookups: {
          [ITEM_LOOKUPS.department]: departments,
          [ITEM_LOOKUPS.category]: categories,
        },
      }}
      initialValues={initialValues}
      editing={!!subCategory}
      onSave={save}
    />
  );
}
