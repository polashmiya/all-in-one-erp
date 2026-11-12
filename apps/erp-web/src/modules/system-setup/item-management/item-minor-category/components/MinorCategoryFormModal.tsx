import {
  useCreateItemMinorCategory,
  useUpdateItemMinorCategory,
  type FeatureConfig,
  type ItemLookup,
  type ItemMinorCategoryRow,
  type SaveItemMinorCategoryRequest,
} from '@erp/api-client';
import { Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { MasterFormModal, trimmed } from '@/common/master-data';
import { ITEM_LOOKUPS } from '../../item-management';

type MinorCategoryFormValues = SaveItemMinorCategoryRequest & {
  departmentId?: number | null;
  categoryId?: number | null;
};

export function MinorCategoryFormModal({
  open,
  minorCategory,
  config,
  defaults,
  departments,
  categories,
  subCategories,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  minorCategory?: ItemMinorCategoryRow;
  config: FeatureConfig;
  defaults: { departmentId?: number; categoryId?: number; subCategoryId?: number };
  departments?: ItemLookup[];
  categories?: ItemLookup[];
  subCategories?: ItemLookup[];
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<MinorCategoryFormValues>();
  const create = useCreateItemMinorCategory();
  const update = useUpdateItemMinorCategory();
  const initialValues = useMemo<Partial<MinorCategoryFormValues>>(
    () =>
      minorCategory ?? {
        departmentId: defaults.departmentId,
        categoryId: defaults.categoryId,
        subCategoryId: defaults.subCategoryId,
      },
    [minorCategory, defaults.departmentId, defaults.categoryId, defaults.subCategoryId],
  );

  const save = async (values: MinorCategoryFormValues) => {
    const data: SaveItemMinorCategoryRequest = {
      subCategoryId: values.subCategoryId,
      name: values.name.trim(),
      imageSource: trimmed(values.imageSource),
      isActive: values.isActive ?? true,
      customData: values.customData ?? null,
    };
    const saved = minorCategory
      ? await update.mutateAsync({ id: minorCategory.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<MinorCategoryFormValues>
      open={open}
      onClose={onClose}
      title={minorCategory ? t('minor.form.editTitle') : t('minor.form.newTitle')}
      subtitle={t('minor.form.subtitle', { unit: unitName })}
      form={form}
      config={config}
      options={{
        lookups: {
          [ITEM_LOOKUPS.department]: departments,
          [ITEM_LOOKUPS.category]: categories,
          [ITEM_LOOKUPS.subCategory]: subCategories,
        },
      }}
      initialValues={initialValues}
      editing={!!minorCategory}
      onSave={save}
    />
  );
}
