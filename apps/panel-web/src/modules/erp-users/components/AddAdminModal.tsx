import { applyFieldErrors, useAddAccountAdmin } from '@erp/api-client';
import { FormModal, NewPasswordFields, UserProfileFields, type UserProfileValues } from '@erp/ui';
import { Alert, App, Form } from 'antd';
import { useTranslation } from 'react-i18next';

interface AddAdminValues extends UserProfileValues {
  newPassword: string;
  confirmPassword: string;
}

export function AddAdminModal({
  accountId,
  accountName,
  open,
  onClose,
  onAdded,
}: {
  accountId: string;
  accountName: string;
  open: boolean;
  onClose: () => void;
  onAdded: () => unknown;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [form] = Form.useForm<AddAdminValues>();
  const add = useAddAccountAdmin();

  const submit = async ({ newPassword, confirmPassword: _, ...profile }: AddAdminValues) => {
    try {
      await add.mutateAsync({
        id: accountId,
        data: { ...profile, phone: profile.phone || null, password: newPassword },
      });
      void message.success(t('erpUsers.addAdmin.done'));
      await onAdded();
    } catch (error) {
      if (!applyFieldErrors(form, error)) void message.error(t('erpUsers.failed'));
      throw error;
    }
  };

  return (
    <FormModal<AddAdminValues>
      open={open}
      onClose={onClose}
      form={form}
      size="md"
      title={t('erpUsers.addAdmin.title', { account: accountName })}
      submitText={t('erpUsers.addAdmin.submit')}
      addAnotherText={t('erpUsers.addAdmin.another')}
      onSubmit={submit}
    >
      <div className="erp-stack">
        <Alert type="info" showIcon message={t('erpUsers.addAdmin.lead')} />
        <UserProfileFields />
        <NewPasswordFields />
      </div>
    </FormModal>
  );
}
