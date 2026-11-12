import {
  ApiError,
  useSaveSectionSetting,
  type SectionConfigRow,
  type SectionSettingValues,
} from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { Field, FieldGrid, FormModal, YesNoSwitch } from '@erp/ui';
import { App, Form, Input, Select } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  effective,
  FIELD_WIDTHS,
  LABEL_MAX,
  toOverride,
} from '../../account-configuration/field-settings';

export function SectionSettingsModal({
  accountId,
  feature,
  businessUnitId,
  scope,
  row,
  onClose,
  onSaved,
}: {
  accountId: string;
  feature: string;
  businessUnitId?: string;
  scope: string;
  row?: SectionConfigRow;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const text = useText();
  const { message } = App.useApp();
  const [form] = Form.useForm<SectionSettingValues>();
  const save = useSaveSectionSetting();
  const initialValues = useMemo(() => (row ? effective(row) : undefined), [row]);

  if (!row) return null;

  const submit = async (values: SectionSettingValues) => {
    try {
      await save.mutateAsync({
        id: accountId,
        code: feature,
        key: row.key,
        data: toOverride({ ...initialValues, ...values }, row.base),
        params: businessUnitId ? { businessUnitId } : undefined,
      });
      await onSaved();
      void message.success(t('layout.saved'));
    } catch (error) {
      if (error instanceof ApiError && error.status === 400)
        form.setFields(
          Object.entries(error.fieldErrors).map(([name, errors]) => ({
            name: name as keyof SectionSettingValues,
            errors,
          })),
        );
      void message.error(
        error instanceof ApiError
          ? (Object.values(error.fieldErrors)[0]?.[0] ?? error.message)
          : t('layout.failed'),
      );
      throw error;
    }
  };

  return (
    <FormModal<SectionSettingValues>
      open={!!row}
      onClose={onClose}
      form={form}
      title={text({ en: initialValues?.labelEn ?? row.key, bn: initialValues?.labelBn ?? null })}
      subtitle={scope}
      initialValues={initialValues}
      onSubmit={submit}
    >
      <FieldGrid columns={2}>
        <Field name="labelEn" label={t('config.f.labelEn')}>
          <Input maxLength={LABEL_MAX} />
        </Field>
        <Field name="labelBn" label={t('config.f.labelBn')}>
          <Input maxLength={LABEL_MAX} />
        </Field>
        <Field name="isVisible" label={t('config.f.visible')} valuePropName="checked">
          <YesNoSwitch />
        </Field>
        {!row.isTab && (
          <Field name="columns" label={t('layout.f.columns')}>
            <Select
              options={FIELD_WIDTHS.map((count) => ({
                value: count,
                label: t('config.f.columns', { count }),
              }))}
            />
          </Field>
        )}
      </FieldGrid>
    </FormModal>
  );
}
