import { PlusOutlined, TeamOutlined } from '@ant-design/icons';
import { useListErpUsers } from '@erp/api-client';
import { DetailSection, EmptyState, InitialsTile, TitleCell } from '@erp/ui';
import { Button, Flex } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { AddAdminModal } from '@/modules/erp-users/components/AddAdminModal';
import { UserStatus } from '@/modules/erp-users/components/UnitRoles';
import { useIsSuperAdmin } from '@/modules/erp-users/components/useErpUserActions';
import { ACCOUNT_ADMINS_PAGE_SIZE, ERP_USER_PATHS } from '@/modules/erp-users/constants';

export function AccountAdmins({
  accountId,
  accountName,
}: {
  accountId: string;
  accountName: string;
}) {
  const { t } = useTranslation();
  const canChange = useIsSuperAdmin();
  const [adding, setAdding] = useState(false);
  const query = useListErpUsers({ accountId, type: 'admins', pageSize: ACCOUNT_ADMINS_PAGE_SIZE });
  const admins = query.data?.items ?? [];

  return (
    <DetailSection
      title={t('erpUsers.accountUsers')}
      description={t('erpUsers.accountUsersLead')}
      extra={
        <Flex gap={8} wrap>
          <Link to={ERP_USER_PATHS.forAccount(accountId)}>
            <Button icon={<TeamOutlined />}>{t('erpUsers.accountUsersAll')}</Button>
          </Link>
          {canChange && (
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setAdding(true)}>
              {t('erpUsers.addAdmin.action')}
            </Button>
          )}
        </Flex>
      }
    >
      {admins.length === 0 ? (
        <EmptyState title={t('erpUsers.noAdmins')} />
      ) : (
        <Flex vertical gap={12}>
          {admins.map((admin, index) => (
            <Flex key={admin.id} align="center" justify="space-between" gap={12} wrap>
              <Link to={ERP_USER_PATHS.detail(admin.id)}>
                <TitleCell
                  leading={<InitialsTile name={admin.fullName} index={index} />}
                  title={admin.fullName}
                  subtitle={`@${admin.username} · ${admin.email}`}
                />
              </Link>
              <UserStatus user={admin} />
            </Flex>
          ))}
        </Flex>
      )}
      <AddAdminModal
        accountId={accountId}
        accountName={accountName}
        open={adding}
        onClose={() => setAdding(false)}
        onAdded={() => query.refetch()}
      />
    </DetailSection>
  );
}
