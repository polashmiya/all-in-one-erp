import type { ErpUserListItem } from '@erp/api-client';
import { StatusTag } from '@erp/ui';
import { Flex } from 'antd';
import { useTranslation } from 'react-i18next';

export function UnitRoles({ user }: { user: ErpUserListItem }) {
  const { t } = useTranslation();

  if (user.isAccountAdmin) return <StatusTag tone="violet">{t('erpUsers.accountAdmin')}</StatusTag>;

  return (
    <Flex gap={4} wrap>
      {user.units.map((unit) => (
        <StatusTag key={unit.businessUnitId} tone={unit.roleName ? 'neutral' : 'warning'}>
          {unit.businessUnitName} · {unit.roleName ?? t('erpUsers.noRole')}
        </StatusTag>
      ))}
    </Flex>
  );
}

export function UserStatus({ user }: { user: ErpUserListItem }) {
  const { t } = useTranslation();

  return (
    <Flex gap={4} wrap>
      <StatusTag tone={user.isActive ? 'success' : 'neutral'} dot>
        {user.isActive ? t('erpUsers.active') : t('erpUsers.inactive')}
      </StatusTag>
      {user.isLocked && <StatusTag tone="danger">{t('erpUsers.locked')}</StatusTag>}
    </Flex>
  );
}
