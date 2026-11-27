import {
  ApartmentOutlined,
  CalendarOutlined,
  EditOutlined,
  IdcardOutlined,
  PhoneOutlined,
  UserOutlined,
} from '@ant-design/icons';
import {
  getGetEmployeeQueryKey,
  problemMessage,
  useGetEmployee,
  useSetEmployeesStatus,
  type EmployeeDetail,
  type FeatureConfig,
} from '@erp/api-client';
import { useViewTabs } from '@erp/metadata-engine';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatDateTime,
  StatusTag,
} from '@erp/ui';
import { App } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import {
  EMPLOYEE_FEATURE,
  employeePaths,
  USER_FEATURE,
  userPaths,
  useRefreshUserManagement,
} from '../../user-management';

export function EmployeeViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(EMPLOYEE_FEATURE);
  const config = useFeatureConfig(EMPLOYEE_FEATURE, access.view);
  const query = useGetEmployee(id, {
    query: { queryKey: useUnitKey(getGetEmployeeQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  if (!query.data || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('employee.notFound')}
        missingActions={[{ key: 'back', label: t('employee.backToList'), to: employeePaths.list }]}
      />
    );
  }

  return <EmployeeView employee={query.data} config={config.data} />;
}

function EmployeeView({ employee, config }: { employee: EmployeeDetail; config: FeatureConfig }) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const access = useFeatureAccess(EMPLOYEE_FEATURE);
  const userAccess = useFeatureAccess(USER_FEATURE);
  const setStatus = useSetEmployeesStatus();
  const refresh = useRefreshUserManagement();
  const tabs = useViewTabs(config, employee, {}, useCustomLookups(config));

  const toggleStatus = async () => {
    try {
      await setStatus.mutateAsync({ data: { ids: [employee.id], isActive: !employee.isActive } });
      await refresh();
      void message.success(
        t(employee.isActive ? 'master.deactivated' : 'master.activated', { count: 1 }),
      );
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  const recordInfo = (
    <DetailSection title={t('employee.side.recordInfo')}>
      <DescriptionList
        items={[
          {
            key: 'user',
            label: t('employee.col.user'),
            value: employee.userId ? (
              <Link to={userPaths.edit(employee.userId)}>@{employee.username}</Link>
            ) : null,
          },
          {
            key: 'created',
            label: t('employee.side.created'),
            value: formatDateTime(employee.createdAt),
          },
          {
            key: 'updated',
            label: t('employee.side.updated'),
            value: employee.modifiedAt ? formatDateTime(employee.modifiedAt) : null,
          },
        ]}
      />
    </DetailSection>
  );

  return (
    <DetailsPageLayout
      crumbs={[employee.code]}
      header={{
        backTo: employeePaths.list,
        icon: employee.imageSource ? <img src={employee.imageSource} alt="" /> : <UserOutlined />,
        title: employee.name,
        tags: (
          <>
            <StatusTag tone={employee.isActive ? 'success' : 'neutral'} dot>
              {employee.isActive ? t('master.active') : t('master.inactive')}
            </StatusTag>
            {employee.designationName && (
              <StatusTag tone="violet">{employee.designationName}</StatusTag>
            )}
            {employee.userId && <StatusTag tone="info">@{employee.username}</StatusTag>}
          </>
        ),
        meta: [
          {
            key: 'code',
            icon: <IdcardOutlined />,
            label: [employee.code, employee.nameBn].filter(Boolean).join(' · '),
          },
          {
            key: 'unit',
            icon: <ApartmentOutlined />,
            label: [employee.homeBusinessUnitName, employee.officeName].filter(Boolean).join(' › '),
          },
          {
            key: 'mobile',
            icon: <PhoneOutlined />,
            label: employee.mobile ?? employee.email ?? '—',
          },
          {
            key: 'joined',
            icon: <CalendarOutlined />,
            label: employee.joiningDate
              ? t('employee.v.joined', { date: formatDate(employee.joiningDate) })
              : t('employee.v.noJoiningDate'),
          },
        ],
        actions: [
          {
            key: 'edit',
            label: t('master.edit'),
            icon: <EditOutlined />,
            variant: 'primary',
            hidden: !access.edit,
            to: employeePaths.edit(employee.id),
          },
          {
            key: 'more',
            label: t('common.moreActions'),
            menu: [
              {
                key: 'user',
                label: employee.userId ? t('employee.openUser') : t('employee.makeUser'),
                hidden: !employee.userId && !userAccess.create,
                onClick: () =>
                  navigate(
                    employee.userId
                      ? userPaths.edit(employee.userId)
                      : userPaths.createFor(employee.id),
                  ),
              },
              {
                key: 'toggle',
                label: employee.isActive ? t('master.deactivate') : t('master.activate'),
                hidden: !access.edit,
                onClick: toggleStatus,
              },
            ],
          },
        ],
      }}
      tabs={tabs.map((tab, index) =>
        index === tabs.length - 1
          ? { ...tab, sections: [...tab.sections, { key: 'record', content: recordInfo }] }
          : tab,
      )}
    />
  );
}
