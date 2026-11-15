import { CloseOutlined, FileTextOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
import { App, Form, type FormInstance } from 'antd';
import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useHotkey } from '../../hooks/useHotkey';
import type { ActionItem } from '../actions/types';
import type { FormDraft } from '../../layout/templates/drafts';
import { StatusTag } from '../data-display/StatusTag';
import { AppModal, type OverlaySize } from './AppModal';

export interface FormModalProps<T> {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  subtitle?: ReactNode;
  size?: OverlaySize;
  initialValues?: Partial<T>;
  submitText?: string;
  addAnotherText?: string;
  danger?: boolean;
  onSubmit: (values: T) => Promise<unknown> | unknown;
  form?: FormInstance<T>;
  draft?: FormDraft<T>;
  children: ReactNode;
}

export function FormModal<T extends object>({
  open,
  onClose,
  title,
  subtitle,
  size,
  initialValues,
  submitText,
  addAnotherText,
  danger,
  onSubmit,
  form: external,
  draft,
  children,
}: FormModalProps<T>) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [own] = Form.useForm<T>();
  const form = external ?? own;
  const [saving, setSaving] = useState(false);

  const reset = useCallback(() => {
    form.resetFields();
    if (initialValues)
      form.setFieldsValue(initialValues as Parameters<typeof form.setFieldsValue>[0]);
  }, [form, initialValues]);

  useEffect(() => {
    if (open) reset();
  }, [open, reset]);

  const submit = async (addAnother = false) => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      await onSubmit(values);
      if (addAnother) reset();
      else onClose();
    } finally {
      setSaving(false);
    }
  };

  const saveDraft = async () => {
    if (!draft) return;
    try {
      await draft.onSave(form.getFieldsValue(true) as T);
      void message.success(t('common.draftSaved'));
      onClose();
    } catch {
      void message.error(t('common.draftFailed'));
    }
  };

  const save = {
    key: 'ok',
    label: submitText ?? t('common.save'),
    icon: <SaveOutlined />,
    onClick: () => submit().catch(() => undefined),
  };

  const menu: ActionItem[] = [
    { ...save, description: t('common.saveHint') },
    ...(addAnotherText
      ? [
          {
            key: 'another',
            label: addAnotherText,
            icon: <PlusOutlined />,
            description: t('common.saveNewHint'),
            onClick: () => submit(true).catch(() => undefined),
          },
        ]
      : []),
    ...(draft
      ? [
          {
            key: 'draft',
            label: t('common.saveDraft'),
            icon: <FileTextOutlined />,
            description: t('common.saveDraftHint'),
            onClick: saveDraft,
          },
        ]
      : []),
    { key: 'cancel-divider', label: '', divider: true },
    {
      key: 'cancel',
      label: t('common.cancel'),
      icon: <CloseOutlined />,
      danger: true,
      description: t('common.cancelHint'),
      onClick: onClose,
    },
  ];

  const actions: ActionItem[] | undefined =
    addAnotherText || draft
      ? [
          {
            ...save,
            variant: danger ? 'danger' : 'primary',
            loading: saving,
            split: true,
            menu,
          },
        ]
      : undefined;

  useHotkey('s', () => void submit().catch(() => undefined), {
    mod: true,
    enabled: open && !saving,
  });

  return (
    <AppModal
      open={open}
      onClose={onClose}
      title={
        draft?.continuing ? (
          <>
            {title} <StatusTag tone="warning">{t('common.draft')}</StatusTag>
          </>
        ) : (
          title
        )
      }
      subtitle={subtitle}
      size={size}
      actions={actions}
      okText={submitText}
      okLoading={saving}
      danger={danger}
      onOk={() => submit().catch(() => undefined)}
    >
      <Form<T>
        form={form}
        layout="vertical"
        className="erp-form"
        onFinish={() => submit().catch(() => undefined)}
      >
        {children}
      </Form>
    </AppModal>
  );
}
