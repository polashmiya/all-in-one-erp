import {
  useCreateItemCategory,
  useUpdateItemCategory,
  type ItemCategoryRow,
  type ItemLookup,
  type SaveItemCategoryRequest,
} from '@erp/api-client';
import { Field, FieldGrid } from '@erp/ui';
import { Form, Input, Select } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActiveField,
  ImageField,
  MasterFormModal,
  CODE_PATTERN,
  selectOptions,
  toUpper,
} from '@/common/master-data';

interface CategoryFormValues {
  departmentId?: number;
  code: string;
  name: string;
  imageSource?: string | null;
  isActive: boolean;
}

const CODE_MAX = 3;
const NAME_MAX = 100;

export function CategoryFormModal({
  open,
  category,
  defaultDepartmentId,
  departments,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  category?: ItemCategoryRow;
  defaultDepartmentId?: number;
  departments?: ItemLookup[];
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<CategoryFormValues>();
  const create = useCreateItemCategory();
  const update = useUpdateItemCategory();

  const initialValues = useMemo<Partial<CategoryFormValues>>(
    () =>
      category
        ? {
            departmentId: category.departmentId,
            code: category.code,
            name: category.name,
            imageSource: category.imageSource,
            isActive: category.isActive,
          }
        : { departmentId: defaultDepartmentId, isActive: true },
    [category, defaultDepartmentId],
  );

  const save = async (values: CategoryFormValues) => {
    const data: SaveItemCategoryRequest = {
      departmentId: values.departmentId!,
      code: values.code.trim(),
      name: values.name.trim(),
      imageSource: values.imageSource ?? null,
      isActive: values.isActive,
    };
    const saved = category
      ? await update.mutateAsync({ id: category.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<CategoryFormValues>
      open={open}
      onClose={onClose}
      title={category ? t('cat.form.editTitle') : t('cat.form.newTitle')}
      subtitle={t('cat.form.subtitle', { unit: unitName })}
      form={form}
      initialValues={initialValues}
      editing={!!category}
      onSave={save}
    >
      <Field
        name="departmentId"
        label={t('cat.f.department')}
        extra={category && category.subCategories > 0 ? t('cat.f.departmentMoveHint') : undefined}
        rules={[{ required: true, message: t('cat.f.departmentRequired') }]}
      >
        <Select<number>
          showSearch
          optionFilterProp="label"
          placeholder={t('cat.f.departmentPlaceholder')}
          options={selectOptions(departments, category?.departmentId)}
        />
      </Field>
      <FieldGrid columns={2}>
        <Field
          name="code"
          label={t('cat.f.code')}
          extra={t('cat.f.codeHint')}
          normalize={toUpper}
          rules={[
            { required: true, whitespace: true, message: t('cat.f.codeRequired') },
            { max: CODE_MAX, message: t('master.f.maxLength', { count: CODE_MAX }) },
            { pattern: CODE_PATTERN, message: t('master.f.codeInvalid') },
          ]}
        >
          <Input maxLength={CODE_MAX} placeholder={t('cat.f.codePlaceholder')} />
        </Field>
        <Field
          name="name"
          label={t('cat.f.name')}
          rules={[
            { required: true, whitespace: true, message: t('cat.f.nameRequired') },
            { max: NAME_MAX, message: t('master.f.maxLength', { count: NAME_MAX }) },
          ]}
        >
          <Input maxLength={NAME_MAX} placeholder={t('cat.f.namePlaceholder')} />
        </Field>
      </FieldGrid>
      <ImageField />
      <ActiveField />
    </MasterFormModal>
  );
}
