import { UserAddOutlined, UserOutlined } from '@ant-design/icons';
import {
  getListEmployeesQueryKey,
  listEmployees,
  useListEmployees,
  useSetEmployeesStatus,
  type EmployeeRow,
} from '@erp/api-client';
import { fieldMap, useConfigColumns, useText, type LookupItem } from '@erp/metadata-engine';
import { keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useListState, withDraft } from '@erp/ui';
import { Link, useNavigate } from 'react-router';
import { appConfig } from '@/common/config/app.config';

import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import {
  EXPORT_PAGE_SIZE,
  filterOptions,
  idParam,
  listParams,
  MasterListPage,
  RecordNameCell,
  useUnitKey,
  type StatusView,
} from '@/common/master-data';
import {
  EMPLOYEE_FEATURE,
  employeePaths,
  USER_FEATURE,
  userPaths,
  useRefreshUserManagement,
} from '../../user-management';
import { EMPLOYEE_LOOKUPS, USER_FILTER_VALUES } from '../constants';
import { useEmployeeLookups } from '../employee-lookups';

type Filters = {
  designationId: string | null;
  departmentId: string | null;
  officeId: string | null;
  user: string | null;
};

export function EmployeeListPage() {
  const { t } = useTranslation();
  const text = useText();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(EMPLOYEE_FEATURE);
  const userAccess = useFeatureAccess(USER_FEATURE);
  const refresh = useRefreshUserManagement();
  const list = useListState<Filters, StatusView>({
    filters: { designationId: null, departmentId: null, officeId: null, user: null },
    view: 'all',
    pageSize: appConfig.defaultPageSize,
  });

  const params = {
    ...listParams(list.query),
    designationId: idParam(list.filters.designationId),
    departmentId: idParam(list.filters.departmentId),
    officeId: list.filters.officeId ?? undefined,
    user: list.filters.user ?? undefined,
  };
  const query = useListEmployees(params, {
    query: {
      queryKey: useUnitKey(getListEmployeesQueryKey(params)),
      placeholderData: keepPreviousData,
      enabled: access.view,
    },
  });
  const config = useFeatureConfig(EMPLOYEE_FEATURE, access.view);
  const lookups = useEmployeeLookups(access.view);
  const setStatus = useSetEmployeesStatus();

  const inactive = (name: string) => t('master.inactiveOption', { name });
  const fields = config.data ? fieldMap(config.data) : undefined;
  const sources: Record<Exclude<keyof Filters, 'user'>, LookupItem[] | undefined> = {
    designationId: lookups[EMPLOYEE_LOOKUPS.designation],
    departmentId: lookups[EMPLOYEE_LOOKUPS.department],
    officeId: lookups[EMPLOYEE_LOOKUPS.office]?.filter((o) => o.parentId === unit?.id),
  };
  const filters = [
    ...(config.data?.filters ?? [])
      .filter((key): key is keyof typeof sources => key in sources)
      .map((key) => ({
        key,
        label: text(fields?.get(key)?.label) ?? key,
        options: filterOptions(sources[key], inactive),
      })),
    {
      key: 'user' as const,
      label: t('employee.col.user'),
      options: USER_FILTER_VALUES.map((value) => ({
        value,
        label: t(`employee.userFilter.${value}`),
      })),
    },
  ];

  const { columns, csvColumns } = useConfigColumns<EmployeeRow>(config.data, {
    name: {
      render: (row, index) => (
        <RecordNameCell
          name={row.name}
          image={row.imageSource}
          index={index}
          subtitle={row.nameBn}
          onOpen={() => navigate(employeePaths.view(row.id))}
        />
      ),
    },
  });

  const userColumn = {
    key: 'user',
    title: t('employee.col.user'),
    render: (_: unknown, row: EmployeeRow) =>
      row.userId ? <Link to={userPaths.edit(row.userId)}>@{row.username}</Link> : '—',
  };

  return (
    <MasterListPage<EmployeeRow, Filters>
      feature={EMPLOYEE_FEATURE}
      title={t('employee.title')}
      subtitle={t('employee.subtitle', { unit: unit?.name ?? '' })}
      newLabel={t('employee.new')}
      search={{ label: t('employee.searchLabel'), placeholder: t('employee.search') }}
      list={list}
      filters={filters}
      data={query.data}
      loading={query.isFetching || config.isPending}
      onRefresh={() => void query.refetch()}
      columns={[...columns, userColumn]}
      csvColumns={[...csvColumns, { header: t('employee.col.user'), value: (row) => row.username }]}
      exportFile={t('employee.exportFile')}
      fetchAll={() =>
        listEmployees({ ...params, page: 1, pageSize: EXPORT_PAGE_SIZE }).then((r) => r.items)
      }
      empty={{ title: t('employee.emptyTitle'), description: t('employee.emptyText') }}
      onCreate={(draftId) => navigate(withDraft(employeePaths.create, draftId))}
      onEdit={(row) => navigate(employeePaths.edit(row.id))}
      onView={(row) => navigate(employeePaths.view(row.id))}
      onSetStatus={(ids, isActive) =>
        setStatus.mutateAsync({ data: { ids, isActive } }).then(refresh)
      }
      moreActions={(row) => [
        row.userId
          ? {
              key: 'open-user',
              label: t('employee.openUser'),
              icon: <UserOutlined />,
              onClick: () => navigate(userPaths.edit(row.userId!)),
            }
          : {
              key: 'make-user',
              label: t('employee.makeUser'),
              icon: <UserAddOutlined />,
              hidden: !userAccess.create,
              onClick: () => navigate(userPaths.createFor(row.id)),
            },
      ]}
    />
  );
}
