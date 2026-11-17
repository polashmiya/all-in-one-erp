import { PlusOutlined, SaveOutlined } from '@ant-design/icons';
import {
  applyFieldErrors,
  problemMessage,
  type DraftState,
  type FeatureConfig,
} from '@erp/api-client';
import { defaultValues, useFormSections, type LookupSources } from '@erp/metadata-engine';
import { FormPageLayout, useGoBack } from '@erp/ui';
import { App, Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import {
  PARTNER_ASIDE_SECTIONS,
  useRefreshPartners,
  type PartnerPaths,
} from '../partner-management';

type Saved = { id: number; code: string };

export interface PartnerFormProps<T extends object> {
  config: FeatureConfig;
  record?: T & Saved & { isActive: boolean };
  draft: DraftState<T>;
  lookups: LookupSources;
  paths: PartnerPaths;
  texts: { newTitle: string; editTitle: string; save: string; saved: (code: string) => string };
  saving: boolean;
  onSave: (values: T, id?: number) => Promise<Saved>;
}

export function PartnerForm<T extends object>({
  config,
  record,
  draft,
  lookups,
  paths,
  texts,
  saving,
  onSave,
}: PartnerFormProps<T>) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [form] = Form.useForm<T>();
  const refresh = useRefreshPartners();
  const goBack = useGoBack(record ? paths.view(record.id) : paths.list);
  const allLookups = { ...useCustomLookups(config), ...lookups };

  const initialValues = useMemo<Partial<T>>(
    () => record ?? { ...(defaultValues(config) as Partial<T>), ...draft.values },
    [record, config, draft.values],
  );

  const { sections, aside } = useFormSections(config, {
    lookups: allLookups,
    aside: PARTNER_ASIDE_SECTIONS,
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
          ? record.isActive
            ? { tone: 'success', label: t('master.active') }
            : { tone: 'neutral', label: t('master.inactive') }
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
          label: t('partner.saveNew'),
          icon: <PlusOutlined />,
          description: t('common.saveNewHint'),
          hidden: !!record,
        },
        { key: 'save', label: texts.save, icon: <SaveOutlined />, variant: 'primary' },
      ]}
      sections={sections}
      aside={aside}
    />
  );
}
