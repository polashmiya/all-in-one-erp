import {
  ApiError,
  useCreateCustomField,
  useListLookupTypes,
  type ConfigSectionOption,
  type CreateCustomFieldRequest,
} from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { Field, FieldGrid, FormModal } from '@erp/ui';
import { App, Form, Input, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { LABEL_MAX, LOOKUP_TYPE } from '../field-settings';

export function CustomFieldModal({
  accountId,
  feature,
  open,
  types,
  sections,
  onClose,
  onSaved,
}: {
  accountId: string;
  feature: string;
  open: boolean;
  types: string[];
  sections: ConfigSectionOption[];
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const text = useText();
  const { message } = App.useApp();
  const [form] = Form.useForm<CreateCustomFieldRequest>();
  const type = Form.useWatch('type', form);
  const create = useCreateCustomField();
  const lists = useListLookupTypes();

  const submit = async (data: CreateCustomFieldRequest) => {
    try {
      await create.mutateAsync({ id: accountId, code: feature, data });
      await onSaved();
      void message.success(t('config.custom.saved'));
    } catch (error) {
      if (error instanceof ApiError && error.status === 400)
        form.setFields(
          Object.entries(error.fieldErrors).map(([name, errors]) => ({
            name: name as keyof CreateCustomFieldRequest,
            errors,
          })),
        );
      void message.error(
        error instanceof ApiError
          ? (Object.values(error.fieldErrors)[0]?.[0] ?? error.message)
          : t('config.failed'),
      );
      throw error;
    }
  };

  return (
    <FormModal<CreateCustomFieldRequest>
      open={open}
      onClose={onClose}
      form={form}
      title={t('config.custom.new')}
      subtitle={t('config.custom.hint')}
      addAnotherText={t('config.custom.addAnother')}
      onSubmit={submit}
    >
      <FieldGrid columns={2}>
        <Field
          name="labelEn"
          label={t('config.f.labelEn')}
          rules={[{ required: true, whitespace: true, message: t('config.custom.labelRequired') }]}
        >
          <Input maxLength={LABEL_MAX} />
        </Field>
        <Field name="labelBn" label={t('config.f.labelBn')}>
          <Input maxLength={LABEL_MAX} />
        </Field>
        <Field
          name="type"
          label={t('config.col.type')}
          rules={[{ required: true, message: t('config.custom.typeRequired') }]}
        >
          <Select options={types.map((key) => ({ value: key, label: t(`config.type.${key}`) }))} />
        </Field>
        <Field
          name="sectionKey"
          label={t('config.f.section')}
          rules={[{ required: true, message: t('config.custom.sectionRequired') }]}
        >
          <Select
            showSearch
            optionFilterProp="label"
            options={sections.map((s) => ({
              value: s.key,
              label: `${text(s.tabLabel)} › ${text(s.label)}`,
            }))}
          />
        </Field>
        {type === LOOKUP_TYPE && (
          <Field
            name="lookup"
            label={t('config.custom.list')}
            rules={[{ required: true, message: t('config.custom.listRequired') }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              loading={lists.isPending}
              options={(lists.data ?? []).map((list) => ({
                value: list.key,
                label: text(list.name) ?? list.key,
              }))}
            />
          </Field>
        )}
      </FieldGrid>
    </FormModal>
  );
}
