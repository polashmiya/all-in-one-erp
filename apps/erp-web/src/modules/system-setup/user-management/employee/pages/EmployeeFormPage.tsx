import { PlusOutlined, SaveOutlined } from '@ant-design/icons';
import {
  applyFieldErrors,
  getGetEmployeeQueryKey,
  problemMessage,
  useCreateEmployee,
  useGetEmployee,
  useUpdateEmployee,
  type DraftState,
  type EmployeeDetail,
  type FeatureConfig,
} from '@erp/api-client';
import { defaultValues, useFormSections } from '@erp/metadata-engine';
import { ForbiddenResult, FormPageLayout, PageState, useGoBack } from '@erp/ui';
import { App, Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { ImageSourceInput, useUnitKey } from '@/common/master-data';
import { useAuthStore } from '@/common/store/auth-store';
import { EMPLOYEE_FEATURE, employeePaths, useRefreshUserManagement } from '../../user-management';
import { EmployeeUserCard } from '../components/EmployeeUserCard';
import { EMPLOYEE_ASIDE_SECTIONS } from '../constants';
import { employeeBehaviors, toRequest, type EmployeeFormValues } from '../employee-form';
import { useEmployeeLookups } from '../employee-lookups';

export function EmployeeFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const access = useFeatureAccess(EMPLOYEE_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(EMPLOYEE_FEATURE, allowed);
  const draft = useFormDraft<EmployeeFormValues>(EMPLOYEE_FEATURE, allowed && !id);
  const source = useGetEmployee(id, {
    query: { queryKey: useUnitKey(getGetEmployeeQueryKey(id)), enabled: allowed && id > 0 },
  });

  if (!allowed) return <ForbiddenResult />;

  const loading = (id > 0 && source.isPending) || config.isPending || draft.loading;

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !source.data) || !config.data)}
      missingTitle={t('employee.notFound')}
      missingActions={[{ key: 'back', label: t('employee.backToList'), to: employeePaths.list }]}
    >
      {config.data && (
        <EmployeeForm
          key={id || 'new'}
          config={config.data}
          employee={id ? source.data : undefined}
          draft={draft}
        />
      )}
    </PageState>
  );
}

function EmployeeForm({
  config,
  employee,
  draft,
}: {
  config: FeatureConfig;
  employee?: EmployeeDetail;
  draft: DraftState<EmployeeFormValues>;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [form] = Form.useForm<EmployeeFormValues>();
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const lookups = { ...useCustomLookups(config), ...useEmployeeLookups() };
  const create = useCreateEmployee();
  const update = useUpdateEmployee();
  const refresh = useRefreshUserManagement();
  const goBack = useGoBack(employee ? employeePaths.view(employee.id) : employeePaths.list);

  const initialValues = useMemo<Partial<EmployeeFormValues>>(
    () =>
      employee ?? {
        ...(defaultValues(config) as Partial<EmployeeFormValues>),
        homeBusinessUnitId: unitId ?? undefined,
        ...draft.values,
      },
    [employee, config, unitId, draft.values],
  );

  const { sections, aside } = useFormSections(config, {
    lookups,
    aside: EMPLOYEE_ASIDE_SECTIONS,
    behaviors: employeeBehaviors(employee?.id),
    widgets: {
      imageSource: () => <ImageSourceInput />,
      signatureImage: () => <ImageSourceInput />,
    },
  });

  const submit = async (values: EmployeeFormValues, action: string) => {
    try {
      const data = toRequest(values);
      const saved = employee
        ? await update.mutateAsync({ id: employee.id, data })
        : await create.mutateAsync({ data });
      const fromDraft = draft.draft.continuing;
      await draft.finish();
      await refresh();
      void message.success(t('employee.form.saved', { code: saved.code }));
      if (action === 'new') {
        if (fromDraft) void navigate(employeePaths.create);
        else form.resetFields();
      } else if (employee) goBack();
      else void navigate(employeePaths.view(saved.id), { replace: true });
    } catch (error) {
      void message.error(
        applyFieldErrors(form, error)
          ? t('master.fixFields')
          : problemMessage(error, t('master.failed')),
      );
      throw error;
    }
  };

  return (
    <FormPageLayout<EmployeeFormValues>
      form={form}
      initialValues={initialValues}
      title={employee ? t('employee.form.editTitle') : t('employee.form.newTitle')}
      status={
        employee
          ? employee.isActive
            ? { tone: 'success', label: t('master.active') }
            : { tone: 'neutral', label: t('master.inactive') }
          : undefined
      }
      backTo={employee ? employeePaths.view(employee.id) : employeePaths.list}
      crumbs={[employee ? employee.code : t('employee.form.newTitle')]}
      cancelTo={employee ? employeePaths.view(employee.id) : employeePaths.list}
      saving={create.isPending || update.isPending}
      onSubmit={submit}
      draft={employee ? undefined : draft.draft}
      submitActions={[
        {
          key: 'new',
          label: t('employee.form.saveNew'),
          icon: <PlusOutlined />,
          description: t('common.saveNewHint'),
          hidden: !!employee,
        },
        {
          key: 'save',
          label: t('employee.form.save'),
          icon: <SaveOutlined />,
          variant: 'primary',
        },
      ]}
      sections={sections}
      aside={[...aside, { key: 'user', content: <EmployeeUserCard employee={employee} /> }]}
    />
  );
}
