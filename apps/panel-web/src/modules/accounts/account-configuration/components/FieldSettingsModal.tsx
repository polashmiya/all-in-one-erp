import {
  ApiError,
  useSaveFieldSetting,
  type ConfigSectionOption,
  type FieldConfigRow,
  type FieldSettingValues,
} from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { Field, FieldSection, FormModal, YesNoSwitch } from '@erp/ui';
import { App, Form, Input, InputNumber, Select } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { effective, FIELD_WIDTHS, NUMBER_TYPES, TEXT_TYPES, toOverride } from '../field-settings';

const LABEL_MAX = 100;
const HELP_MAX = 300;
const DEFAULT_MAX = 200;

export function FieldSettingsModal({
  accountId,
  feature,
  businessUnitId,
  scope,
  row,
  sections,
  onClose,
  onSaved,
}: {
  accountId: string;
  feature: string;
  businessUnitId?: string;
  scope: string;
  row?: FieldConfigRow;
  sections: ConfigSectionOption[];
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const text = useText();
  const { message } = App.useApp();
  const [form] = Form.useForm<FieldSettingValues>();
  const save = useSaveFieldSetting();
  const initialValues = useMemo(() => (row ? effective(row) : undefined), [row]);

  if (!row) return null;

  const locked = row.isLocked;
  const isText = TEXT_TYPES.includes(row.type);
  const isNumber = NUMBER_TYPES.includes(row.type);
  const hasDefault = isText || isNumber || row.type === 'lookup' || row.type === 'boolean';
  const numberProps = {
    getValueProps: (value: unknown) => ({ value: value == null ? undefined : Number(value) }),
    normalize: (value: number | null) => (value == null ? null : String(value)),
  };

  const submit = async (values: FieldSettingValues) => {
    try {
      await save.mutateAsync({
        id: accountId,
        code: feature,
        key: row.key,
        data: toOverride(values, row.base),
        params: businessUnitId ? { businessUnitId } : undefined,
      });
      await onSaved();
      void message.success(t('config.saved'));
    } catch (error) {
      if (error instanceof ApiError && error.status === 400)
        form.setFields(
          Object.entries(error.fieldErrors).map(([name, errors]) => ({
            name: name as keyof FieldSettingValues,
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
    <FormModal<FieldSettingValues>
      open={!!row}
      onClose={onClose}
      size="lg"
      form={form}
      title={text({ en: initialValues?.labelEn ?? row.key, bn: initialValues?.labelBn ?? null })}
      subtitle={scope}
      initialValues={initialValues}
      onSubmit={submit}
    >
      <FieldSection title={t('config.sec.display')} columns={3}>
        <Field
          name="isVisible"
          label={t('config.f.visible')}
          valuePropName="checked"
          extra={locked ? t('config.f.lockedHint') : undefined}
        >
          <YesNoSwitch disabled={locked} />
        </Field>
        <Field name="isRequired" label={t('config.f.required')} valuePropName="checked">
          <YesNoSwitch disabled={locked} />
        </Field>
        <Field name="isReadOnly" label={t('config.f.readOnly')} valuePropName="checked">
          <YesNoSwitch disabled={locked} />
        </Field>
      </FieldSection>

      <FieldSection title={t('config.sec.texts')} columns={2}>
        <Field name="labelEn" label={t('config.f.labelEn')} rules={[{ max: LABEL_MAX }]}>
          <Input maxLength={LABEL_MAX} />
        </Field>
        <Field name="labelBn" label={t('config.f.labelBn')} rules={[{ max: LABEL_MAX }]}>
          <Input maxLength={LABEL_MAX} />
        </Field>
        <Field name="helpEn" label={t('config.f.helpEn')}>
          <Input maxLength={HELP_MAX} />
        </Field>
        <Field name="helpBn" label={t('config.f.helpBn')}>
          <Input maxLength={HELP_MAX} />
        </Field>
        <Field name="placeholderEn" label={t('config.f.placeholderEn')}>
          <Input maxLength={LABEL_MAX} />
        </Field>
        <Field name="placeholderBn" label={t('config.f.placeholderBn')}>
          <Input maxLength={LABEL_MAX} />
        </Field>
      </FieldSection>

      <FieldSection title={t('config.sec.layout')} columns={3}>
        <Field name="sectionKey" label={t('config.f.section')}>
          <Select
            showSearch
            optionFilterProp="label"
            options={sections.map((s) => ({
              value: s.key,
              label: `${text(s.tabLabel)} › ${text(s.label)}`,
            }))}
          />
        </Field>
        <Field name="sortOrder" label={t('config.f.order')}>
          <InputNumber min={0} precision={0} className="erp-w-full" />
        </Field>
        <Field name="colSpan" label={t('config.f.width')}>
          <Select
            options={FIELD_WIDTHS.map((count) => ({
              value: count,
              label: t('config.f.columns', { count }),
            }))}
          />
        </Field>
      </FieldSection>

      {(hasDefault || isText || isNumber) && (
        <FieldSection title={t('config.sec.values')} columns={3}>
          {hasDefault &&
            (row.type === 'boolean' ? (
              <Field
                name="defaultValue"
                label={t('config.f.default')}
                getValueProps={(value: unknown) => ({ checked: value === 'true' })}
                normalize={(value: boolean) => String(value)}
              >
                <YesNoSwitch />
              </Field>
            ) : isText ? (
              <Field name="defaultValue" label={t('config.f.default')}>
                <Input maxLength={DEFAULT_MAX} />
              </Field>
            ) : (
              <Field name="defaultValue" label={t('config.f.default')} {...numberProps}>
                <InputNumber className="erp-w-full" />
              </Field>
            ))}
          {isText && (
            <Field
              name="maxLength"
              label={t('config.f.maxLength')}
              extra={
                row.maxLengthLimit != null
                  ? t('config.f.upTo', { count: row.maxLengthLimit })
                  : undefined
              }
            >
              <InputNumber
                min={1}
                max={row.maxLengthLimit ?? undefined}
                precision={0}
                className="erp-w-full"
              />
            </Field>
          )}
          {isNumber && (
            <>
              <Field name="minValue" label={t('config.f.min')}>
                <InputNumber min={row.minLimit ?? undefined} className="erp-w-full" />
              </Field>
              <Field name="maxValue" label={t('config.f.max')}>
                <InputNumber max={row.maxLimit ?? undefined} className="erp-w-full" />
              </Field>
            </>
          )}
        </FieldSection>
      )}

      {(row.canList || row.canFilter) && (
        <FieldSection title={t('config.sec.list')} columns={3}>
          {row.canList && (
            <>
              <Field name="inList" label={t('config.f.inList')} valuePropName="checked">
                <YesNoSwitch />
              </Field>
              <Field name="listOrder" label={t('config.f.listOrder')}>
                <InputNumber min={0} precision={0} className="erp-w-full" />
              </Field>
            </>
          )}
          {row.canFilter && (
            <Field name="inFilter" label={t('config.f.inFilter')} valuePropName="checked">
              <YesNoSwitch />
            </Field>
          )}
        </FieldSection>
      )}
    </FormModal>
  );
}
