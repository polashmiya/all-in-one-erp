import { PlusOutlined, SaveOutlined } from '@ant-design/icons';
import {
  applyFieldErrors,
  problemMessage,
  type DraftState,
  type FeatureConfig,
} from '@erp/api-client';
import { useFormSections, type FormOptions } from '@erp/metadata-engine';
import { FormPageLayout, useGoBack } from '@erp/ui';
import { App, Form } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { STATUS_TONES, useRefreshProcurement, type DocumentPaths } from '../procurement';

type Saved = { id: number; code: string };

export interface DocumentFormProps<T extends object> {
  config: FeatureConfig;
  record?: Saved & { status: string };
  draft: DraftState<T>;
  initialValues: Partial<T>;
  options: FormOptions;
  lines: ReactNode;
  paths: DocumentPaths;
  texts: { newTitle: string; editTitle: string; save: string; saved: (code: string) => string };
  saving: boolean;
  onSave: (values: T, id?: number) => Promise<Saved>;
}

export function DocumentForm<T extends object>({
  config,
  record,
  draft,
  initialValues,
  options,
  lines,
  paths,
  texts,
  saving,
  onSave,
}: DocumentFormProps<T>) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [form] = Form.useForm<T>();
  const refresh = useRefreshProcurement();
  const goBack = useGoBack(record ? paths.view(record.id) : paths.list);
  const customLookups = useCustomLookups(config);
  const { sections } = useFormSections(config, {
    ...options,
    lookups: { ...customLookups, ...options.lookups },
  });

  const submit = async (values: T, action: string) => {
    try {
      const saved = await onSave(values, record?.id);
      const fromDraft = draft.draft.continuing;
      await draft.finish();
      await refresh();
      void message.success(texts.saved(saved.code));
      if (action === 'new') {
        if (fromDraft) void navigate(paths.create);
        else form.resetFields();
      } else if (record) goBack();
      else void navigate(paths.view(saved.id), { replace: true });
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
    <FormPageLayout<T>
      form={form}
      initialValues={initialValues}
      title={record ? texts.editTitle : texts.newTitle}
      status={
        record
          ? {
              tone: STATUS_TONES[record.status] ?? 'neutral',
              label: t(`procurement.status.${record.status}`),
            }
          : undefined
      }
      backTo={record ? paths.view(record.id) : paths.list}
      crumbs={[record ? record.code : texts.newTitle]}
      cancelTo={record ? paths.view(record.id) : paths.list}
      saving={saving}
      onSubmit={submit}
      draft={record ? undefined : draft.draft}
      submitActions={[
        {
          key: 'new',
          label: t('procurement.saveNew'),
          icon: <PlusOutlined />,
          description: t('common.saveNewHint'),
          hidden: !!record,
        },
        { key: 'save', label: texts.save, icon: <SaveOutlined />, variant: 'primary' },
      ]}
      sections={[...sections, { key: 'lines', fields: ['lines'], content: lines }]}
    />
  );
}
