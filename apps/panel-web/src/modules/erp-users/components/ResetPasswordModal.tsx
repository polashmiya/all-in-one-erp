import { applyFieldErrors, useResetErpUserPassword, type ErpUserListItem } from '@erp/api-client';
import { AppModal, ChoiceCards, NewPasswordFields } from '@erp/ui';
import { Alert, Form, Typography } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

type Mode = 'generate' | 'type';

interface ResetValues {
  newPassword?: string;
  confirmPassword?: string;
}

export function ResetPasswordModal({
  user,
  onClose,
  onDone,
}: {
  user?: ErpUserListItem;
  onClose: () => void;
  onDone: () => unknown;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<ResetValues>();
  const [mode, setMode] = useState<Mode>('generate');
  const [password, setPassword] = useState<string | null>(null);
  const reset = useResetErpUserPassword();

  const close = () => {
    setPassword(null);
    setMode('generate');
    if (mode === 'type') form.resetFields();
    onClose();
  };

  const submit = async () => {
    if (!user) return;
    const values = mode === 'type' ? await form.validateFields() : {};
    try {
      const result = await reset.mutateAsync({
        id: user.id,
        data: { password: values.newPassword ?? null },
      });
      await onDone();
      if (result.generatedPassword) setPassword(result.generatedPassword);
      else close();
    } catch (error) {
      applyFieldErrors(form, error);
    }
  };

  if (password) {
    return (
      <AppModal
        open={!!user}
        onClose={close}
        title={t('erpUsers.reset.result')}
        okText={t('erpUsers.reset.done')}
        onOk={close}
      >
        <div className="erp-stack">
          <Typography.Text copyable code strong>
            {password}
          </Typography.Text>
          <Alert type="warning" showIcon message={t('erpUsers.reset.resultNote')} />
        </div>
      </AppModal>
    );
  }

  return (
    <AppModal
      open={!!user}
      onClose={close}
      title={t('erpUsers.reset.title', { name: user?.fullName ?? '' })}
      subtitle={t('erpUsers.reset.lead')}
      okText={t('erpUsers.reset.submit')}
      okLoading={reset.isPending}
      onOk={submit}
    >
      <div className="erp-stack">
        <ChoiceCards<Mode>
          label={t('erpUsers.reset.submit')}
          value={mode}
          onChange={setMode}
          options={[
            {
              value: 'generate',
              title: t('erpUsers.reset.generate'),
              description: t('erpUsers.reset.generateHelp'),
            },
            {
              value: 'type',
              title: t('erpUsers.reset.type'),
              description: t('erpUsers.reset.typeHelp'),
            },
          ]}
        />
        {mode === 'type' && (
          <Form<ResetValues> form={form} layout="vertical" className="erp-form">
            <NewPasswordFields />
          </Form>
        )}
      </div>
    </AppModal>
  );
}
