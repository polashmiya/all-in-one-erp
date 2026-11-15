import type { FeatureConfig } from '@erp/api-client';
import { ConfigFields, defaultValues, type FormOptions } from '@erp/metadata-engine';
import { App, type FormInstance } from 'antd';
import { use, useMemo, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { FormModal } from '@erp/ui';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useModalDraft } from '@/common/hooks/useDrafts';
import { applyFieldErrors, problemMessage } from '@erp/api-client';
import { MasterDraftContext } from './draft-context';
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
  const { draftId, setDraftId } = use(MasterDraftContext);
  const saved = useModalDraft<V>(config.feature, open, editing, draftId, setDraftId);
  const values = useMemo(
    () => ({ ...defaultValues(config), ...initialValues, ...saved.values }) as Partial<V>,
    [config, initialValues, saved.values],
  );

  const submit = async (data: V) => {
    try {
      const result = await onSave(data);
      await saved.finish();
      void message.success(t(editing ? 'master.updated' : 'master.created', { name: result.name }));
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
      onClose={() => saved.close(onClose)}
      title={title}
      subtitle={subtitle}
      form={form}
      initialValues={values}
      submitText={editing ? t('master.save') : t('master.create')}
      addAnotherText={editing ? undefined : t('master.createAnother')}
      onSubmit={submit}
      draft={saved.draft}
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
