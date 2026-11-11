import {
  useCreateItemMinorCategory,
  useUpdateItemMinorCategory,
  type ItemLookup,
  type ItemMinorCategoryRow,
  type SaveItemMinorCategoryRequest,
} from '@erp/api-client';
import { Field, FieldGrid } from '@erp/ui';
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

interface MinorCategoryFormValues {
  departmentId?: number | null;
  categoryId?: number | null;
  subCategoryId?: number;
  name: string;
  imageSource?: string | null;
  isActive: boolean;
}

const NAME_MAX = 100;

export function MinorCategoryFormModal({
  open,
  minorCategory,
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
  const departmentId = Form.useWatch('departmentId', form);
  const categoryId = Form.useWatch('categoryId', form);
  const create = useCreateItemMinorCategory();
  const update = useUpdateItemMinorCategory();

  const initialValues = useMemo<Partial<MinorCategoryFormValues>>(
    () =>
      minorCategory
        ? {
            departmentId: minorCategory.departmentId,
            categoryId: minorCategory.categoryId,
            subCategoryId: minorCategory.subCategoryId,
            name: minorCategory.name,
            imageSource: minorCategory.imageSource,
            isActive: minorCategory.isActive,
          }
        : {
            departmentId: defaults.departmentId,
            categoryId: defaults.categoryId,
            subCategoryId: defaults.subCategoryId,
            isActive: true,
          },
    [minorCategory, defaults.departmentId, defaults.categoryId, defaults.subCategoryId],
  );

  const save = async (values: MinorCategoryFormValues) => {
    const data: SaveItemMinorCategoryRequest = {
      subCategoryId: values.subCategoryId!,
      name: values.name.trim(),
      imageSource: values.imageSource ?? null,
      isActive: values.isActive,
    };
    const saved = minorCategory
      ? await update.mutateAsync({ id: minorCategory.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  const keepSubCategory = (category: number | null | undefined) => {
    const current = form.getFieldValue('subCategoryId') as number | undefined;
    if (category != null && parentOf(subCategories, current) !== category)
      form.setFieldValue('subCategoryId', undefined);
  };

  const onDepartmentChange = (next?: number | null) => {
    const category = form.getFieldValue('categoryId') as number | undefined;
    if (next == null) return;
    if (parentOf(categories, category) !== next) form.setFieldValue('categoryId', undefined);
    const current = form.getFieldValue('subCategoryId') as number | undefined;
    if (parentOf(categories, parentOf(subCategories, current)) !== next)
      form.setFieldValue('subCategoryId', undefined);
  };

  const onCategoryChange = (next?: number | null) => {
    if (next != null) form.setFieldValue('departmentId', parentOf(categories, next));
    keepSubCategory(next);
  };

  const onSubCategoryChange = (next?: number) => {
    const category = parentOf(subCategories, next);
    form.setFieldsValue({ categoryId: category, departmentId: parentOf(categories, category) });
  };

  const visibleCategories = categories?.filter(
    (c) => departmentId == null || c.parentId === departmentId,
  );

  return (
    <MasterFormModal<MinorCategoryFormValues>
      open={open}
      onClose={onClose}
      title={minorCategory ? t('minor.form.editTitle') : t('minor.form.newTitle')}
      subtitle={t('minor.form.subtitle', { unit: unitName })}
      form={form}
      initialValues={initialValues}
      editing={!!minorCategory}
      onSave={save}
    >
      <FieldGrid columns={2}>
        <Field name="departmentId" label={t('minor.f.department')}>
          <Select<number>
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder={t('minor.f.departmentPlaceholder')}
            options={selectOptions(departments, minorCategory?.departmentId)}
            onChange={onDepartmentChange}
          />
        </Field>
        <Field name="categoryId" label={t('minor.f.category')}>
          <Select<number>
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder={t('minor.f.categoryPlaceholder')}
            options={childSelectOptions(
              categories,
              departments,
              departmentId,
              minorCategory?.categoryId,
            )}
            onChange={onCategoryChange}
          />
        </Field>
      </FieldGrid>
      <Field
        name="subCategoryId"
        label={t('minor.f.subCategory')}
        extra={t('minor.f.filterHint')}
        rules={[{ required: true, message: t('minor.f.subCategoryRequired') }]}
      >
        <Select<number>
          showSearch
          optionFilterProp="label"
          placeholder={t('minor.f.subCategoryPlaceholder')}
          options={childSelectOptions(
            subCategories,
            visibleCategories,
            categoryId,
            minorCategory?.subCategoryId,
          )}
          onChange={onSubCategoryChange}
        />
      </Field>
      <Field
        name="name"
        label={t('minor.f.name')}
        rules={[
          { required: true, whitespace: true, message: t('minor.f.nameRequired') },
          { max: NAME_MAX, message: t('master.f.maxLength', { count: NAME_MAX }) },
        ]}
      >
        <Input maxLength={NAME_MAX} placeholder={t('minor.f.namePlaceholder')} />
      </Field>
      <ImageField />
      <ActiveField />
    </MasterFormModal>
  );
}
