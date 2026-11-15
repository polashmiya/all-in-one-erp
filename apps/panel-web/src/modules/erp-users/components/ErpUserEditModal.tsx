import { applyFieldErrors, useUpdateErpUser, type ErpUserListItem } from '@erp/api-client';
import { FormModal, UserProfileFields, type UserProfileValues } from '@erp/ui';
import { App, Form } from 'antd';
import { useTranslation } from 'react-i18next';

export function ErpUserEditModal({
  user,
  onClose,
  onSaved,
}: {
  user?: ErpUserListItem;
  onClose: () => void;
  onSaved: () => unknown;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [form] = Form.useForm<UserProfileValues>();
  const update = useUpdateErpUser();

  const submit = async (values: UserProfileValues) => {
    if (!user) return;
    try {
      await update.mutateAsync({ id: user.id, data: { ...values, phone: values.phone || null } });
      void message.success(t('erpUsers.done.saved'));
      await onSaved();
    } catch (error) {
      if (!applyFieldErrors(form, error)) void message.error(t('erpUsers.failed'));
      throw error;
    }
  };

  return (
    <FormModal<UserProfileValues>
      open={!!user}
      onClose={onClose}
      form={form}
      size="md"
      title={t('erpUsers.edit.title', { name: user?.fullName ?? '' })}
      initialValues={user}
      onSubmit={submit}
    >
      <UserProfileFields />
    </FormModal>
  );
}
