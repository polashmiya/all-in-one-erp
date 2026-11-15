import { problemMessage, useGetRoleOptions, useSetUserRole, type UserRow } from '@erp/api-client';
import { FormModal } from '@erp/ui';
import { App, Form, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';

type Values = { roleId: string | null };

export function AssignRoleModal({
  user,
  onClose,
  onDone,
}: {
  user?: UserRow;
  onClose: () => void;
  onDone: () => unknown;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const unit = useAuthStore(selectCurrentUnit);
  const roles = useGetRoleOptions({ query: { enabled: !!user } });
  const setRole = useSetUserRole();

  const submit = async ({ roleId }: Values) => {
    if (!user) return;
    try {
      await setRole.mutateAsync({ id: user.id, data: { roleId: roleId ?? null } });
      void message.success(t('user.role.done'));
      await onDone();
    } catch (error) {
      void message.error(problemMessage(error, t('user.failed')));
      throw error;
    }
  };

  return (
    <FormModal<Values>
      open={!!user}
      onClose={onClose}
      size="sm"
      title={t('user.role.title', { name: user?.fullName ?? '' })}
      subtitle={t('user.role.lead', { unit: unit?.name ?? '' })}
      initialValues={{ roleId: user?.roleId ?? null }}
      submitText={t('user.role.submit')}
      onSubmit={submit}
    >
      <Form.Item name="roleId" label={t('user.col.role')}>
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder={t('user.noRole')}
          options={(roles.data ?? []).map((role) => ({ value: role.id, label: role.name }))}
        />
      </Form.Item>
    </FormModal>
  );
}
