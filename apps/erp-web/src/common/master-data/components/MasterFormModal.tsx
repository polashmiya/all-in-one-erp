import { Field, FormModal } from '@erp/ui';
import { App, Switch, type FormInstance } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { applyFieldErrors, problemMessage } from '@/common/utils/api-errors';
import { ImageSourceInput } from './ImageSourceInput';

export function MasterFormModal<V extends object>({
  open,
  onClose,
  title,
  subtitle,
  form,
  initialValues,
  editing,
  onSave,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: ReactNode;
  form: FormInstance<V>;
  initialValues: Partial<V>;
  editing: boolean;
  onSave: (values: V) => Promise<{ name: string }>;
  children: ReactNode;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();

  const submit = async (values: V) => {
    try {
      const saved = await onSave(values);
      void message.success(t(editing ? 'master.updated' : 'master.created', { name: saved.name }));
    } catch (error) {
      void message.error(
        applyFieldErrors(form, error)
          ? t('master.fixFields')
          : problemMessage(error, t('master.failed')),
      );
      throw error;
    }
  };

  return (
    <FormModal<V>
      open={open}
      onClose={onClose}
      title={title}
      subtitle={subtitle}
      form={form}
      initialValues={initialValues}
      submitText={editing ? t('master.save') : t('master.create')}
      addAnotherText={editing ? undefined : t('master.createAnother')}
      onSubmit={submit}
    >
      {children}
    </FormModal>
  );
}

export function ImageField() {
  const { t } = useTranslation();
  return (
    <Field name="imageSource" label={t('master.f.image')}>
      <ImageSourceInput />
    </Field>
  );
}

export function ActiveField() {
  const { t } = useTranslation();
  return (
    <Field
      name="isActive"
      label={t('master.f.active')}
      valuePropName="checked"
      extra={t('master.f.activeHint')}
    >
      <Switch />
    </Field>
  );
}
