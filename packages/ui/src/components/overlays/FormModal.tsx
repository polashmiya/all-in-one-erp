import { Form, type FormInstance } from 'antd';
import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useHotkey } from '../../hooks/useHotkey';
import type { ActionItem } from '../actions/types';
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
  children,
}: FormModalProps<T>) {
  const { t } = useTranslation();
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

  const actions: ActionItem[] | undefined = addAnotherText
    ? [
        { key: 'cancel', label: t('common.cancel'), variant: 'text', onClick: onClose },
        {
          key: 'another',
          label: addAnotherText,
          loading: saving,
          onClick: () => submit(true).catch(() => undefined),
        },
        {
          key: 'ok',
          label: submitText ?? t('common.save'),
          variant: danger ? 'danger' : 'primary',
          loading: saving,
          onClick: () => submit().catch(() => undefined),
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
      title={title}
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
