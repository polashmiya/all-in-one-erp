import type { FeatureConfig } from '@erp/api-client';
import { ConfigFields, defaultValues, type FormOptions } from '@erp/metadata-engine';
import { FormModal } from '@erp/ui';
import { App, type FormInstance } from 'antd';
import { useMemo, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { applyFieldErrors, problemMessage } from '@/common/utils/api-errors';
import { ImageSourceInput } from './ImageSourceInput';

export function MasterFormModal<V extends object>({
  open,
  onClose,
  title,
  subtitle,
  form,
  config,
  options,
  initialValues,
  editing,
  onSave,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: ReactNode;
  form: FormInstance<V>;
  config: FeatureConfig;
  options?: FormOptions;
  initialValues: Partial<V>;
  editing: boolean;
  onSave: (values: V) => Promise<{ name: string }>;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const customLookups = useCustomLookups(config);
  const values = useMemo(
    () => ({ ...defaultValues(config), ...initialValues }) as Partial<V>,
    [config, initialValues],
  );

  const submit = async (data: V) => {
    try {
      const saved = await onSave(data);
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
      initialValues={values}
      submitText={editing ? t('master.save') : t('master.create')}
      addAnotherText={editing ? undefined : t('master.createAnother')}
      onSubmit={submit}
    >
      <ConfigFields
        config={config}
        {...options}
        lookups={{ ...customLookups, ...options?.lookups }}
        widgets={{ imageSource: () => <ImageSourceInput />, ...options?.widgets }}
      />
    </FormModal>
  );
}
