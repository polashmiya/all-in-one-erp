import { CloseOutlined, UserAddOutlined } from '@ant-design/icons';
import {
  getGetUserOptionsQueryKey,
  problemMessage,
  useAssignRoleUsers,
  useGetUserOptions,
  useRemoveRoleUser,
  type RoleDetail,
} from '@erp/api-client';
import { DetailSection, EmptyState, InitialsTile, TitleCell } from '@erp/ui';
import { App, Button, Flex, Select } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';

export function RoleUsers({
  role,
  readOnly,
  onChanged,
}: {
  role: RoleDetail;
  readOnly: boolean;
  onChanged: () => unknown;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const unit = useAuthStore(selectCurrentUnit);
  const me = useAuthStore((s) => s.user?.id);
  const [picked, setPicked] = useState<string[]>([]);
  const options = useGetUserOptions({
    query: { queryKey: useUnitKey(getGetUserOptionsQueryKey()), enabled: !readOnly },
  });
  const assign = useAssignRoleUsers();
  const remove = useRemoveRoleUser();
  const canAssign = !readOnly && role.isActive;

  const run = async (action: () => Promise<unknown>) => {
    try {
      await action();
      await onChanged();
      void options.refetch();
    } catch (error) {
      void message.error(problemMessage(error, t('role.failed')));
    }
  };

  const add = () =>
    run(async () => {
      await assign.mutateAsync({ id: role.id, data: { userIds: picked } });
      setPicked([]);
      void message.success(t('role.usersAdded', { count: picked.length }));
    });

  const choices = (options.data ?? []).filter(
    (user) => user.id !== me && role.users.every((u) => u.userId !== user.id),
  );

  return (
    <DetailSection
      title={t('role.section.users', { unit: unit?.name ?? '' })}
      description={t('role.usersLead')}
    >
      <Flex vertical gap="middle">
        {canAssign && (
          <Flex gap="small">
            <Select
              mode="multiple"
              className="erp-w-full"
              value={picked}
              onChange={setPicked}
              optionFilterProp="label"
              placeholder={t('role.addUsers')}
              options={choices.map((user) => ({
                value: user.id,
                label: `${user.fullName} (@${user.username})`,
              }))}
            />
            <Button
              icon={<UserAddOutlined />}
              disabled={picked.length === 0}
              loading={assign.isPending}
              onClick={() => void add()}
            >
              {t('role.assign')}
            </Button>
          </Flex>
        )}
        {role.users.length === 0 ? (
          <EmptyState title={t('role.noUsers')} />
        ) : (
          role.users.map((user, index) => (
            <Flex key={user.userId} align="center" justify="space-between" gap="small">
              <TitleCell
                leading={<InitialsTile name={user.fullName} index={index} />}
                title={user.fullName}
                subtitle={`@${user.username}`}
              />
              {!readOnly && user.userId !== me && (
                <Button
                  type="text"
                  size="small"
                  icon={<CloseOutlined />}
                  aria-label={t('role.removeUser', { name: user.fullName })}
                  onClick={() =>
                    void run(() => remove.mutateAsync({ id: role.id, userId: user.userId }))
                  }
                />
              )}
            </Flex>
          ))
        )}
      </Flex>
    </DetailSection>
  );
}
