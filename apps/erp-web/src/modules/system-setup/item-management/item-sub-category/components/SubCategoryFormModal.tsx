import {
  useCreateItemSubCategory,
  useUpdateItemSubCategory,
  type ItemLookup,
  type ItemSubCategoryRow,
  type SaveItemSubCategoryRequest,
} from '@erp/api-client';
import { Field } from '@erp/ui';
import { Form, Input, Select } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActiveField,
  ImageField,
  MasterFormModal,
  childSelectOptions,
  parentOf,
  selectOptions,
} from '@/common/master-data';

interface SubCategoryFormValues {
  departmentId?: number | null;
  categoryId?: number;
  name: string;
  imageSource?: string | null;
  isActive: boolean;
}

const NAME_MAX = 100;

export function SubCategoryFormModal({
  open,
  subCategory,
  defaults,
  departments,
  categories,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  subCategory?: ItemSubCategoryRow;
  defaults: { departmentId?: number; categoryId?: number };
  departments?: ItemLookup[];
  categories?: ItemLookup[];
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SubCategoryFormValues>();
  const departmentId = Form.useWatch('departmentId', form);
  const create = useCreateItemSubCategory();
  const update = useUpdateItemSubCategory();

  const initialValues = useMemo<Partial<SubCategoryFormValues>>(
    () =>
      subCategory
        ? {
            departmentId: subCategory.departmentId,
            categoryId: subCategory.categoryId,
            name: subCategory.name,
            imageSource: subCategory.imageSource,
            isActive: subCategory.isActive,
          }
        : { departmentId: defaults.departmentId, categoryId: defaults.categoryId, isActive: true },
    [subCategory, defaults.departmentId, defaults.categoryId],
  );

  const save = async (values: SubCategoryFormValues) => {
    const data: SaveItemSubCategoryRequest = {
      categoryId: values.categoryId!,
      name: values.name.trim(),
      imageSource: values.imageSource ?? null,
      isActive: values.isActive,
    };
    const saved = subCategory
      ? await update.mutateAsync({ id: subCategory.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  const onDepartmentChange = (next?: number | null) => {
    const categoryId = form.getFieldValue('categoryId') as number | undefined;
    if (next != null && parentOf(categories, categoryId) !== next)
      form.setFieldValue('categoryId', undefined);
  };

  return (
    <MasterFormModal<SubCategoryFormValues>
      open={open}
      onClose={onClose}
      title={subCategory ? t('subcat.form.editTitle') : t('subcat.form.newTitle')}
      subtitle={t('subcat.form.subtitle', { unit: unitName })}
      form={form}
      initialValues={initialValues}
      editing={!!subCategory}
      onSave={save}
    >
      <Field
        name="departmentId"
        label={t('subcat.f.department')}
        extra={t('subcat.f.departmentHint')}
      >
        <Select<number>
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder={t('subcat.f.departmentPlaceholder')}
          options={selectOptions(departments, subCategory?.departmentId)}
          onChange={onDepartmentChange}
        />
      </Field>
      <Field
        name="categoryId"
        label={t('subcat.f.category')}
        extra={
          subCategory && subCategory.minorCategories > 0
            ? t('subcat.f.categoryMoveHint')
            : undefined
        }
        rules={[{ required: true, message: t('subcat.f.categoryRequired') }]}
      >
        <Select<number>
          showSearch
          optionFilterProp="label"
          placeholder={t('subcat.f.categoryPlaceholder')}
          options={childSelectOptions(
            categories,
            departments,
            departmentId,
            subCategory?.categoryId,
          )}
          onChange={(id) => form.setFieldValue('departmentId', parentOf(categories, id))}
        />
      </Field>
      <Field
        name="name"
        label={t('subcat.f.name')}
        rules={[
          { required: true, whitespace: true, message: t('subcat.f.nameRequired') },
          { max: NAME_MAX, message: t('master.f.maxLength', { count: NAME_MAX }) },
        ]}
      >
        <Input maxLength={NAME_MAX} placeholder={t('subcat.f.namePlaceholder')} />
      </Field>
      <ImageField />
      <ActiveField />
    </MasterFormModal>
  );
}
