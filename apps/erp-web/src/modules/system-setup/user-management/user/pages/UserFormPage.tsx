import { CheckOutlined } from '@ant-design/icons';
import {
  applyFieldErrors,
  getEmployee,
  getGetEmployeeOptionsQueryKey,
  getGetUserQueryKey,
  problemMessage,
  useCreateUser,
  useGetEmployeeOptions,
  useGetUser,
  useUpdateUser,
  type DraftState,
  type UserDetail,
} from '@erp/api-client';
import { lookupLabel } from '@erp/metadata-engine';
import {
  DescriptionList,
  DetailSection,
  ForbiddenResult,
  FormPageLayout,
  NewPasswordFields,
  PageState,
  StatusTag,
  UserProfileFields,
  useGoBack,
  type UserProfileValues,
} from '@erp/ui';
import { App, Form, Select } from 'antd';
import { useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { idParam, useUnitKey } from '@/common/master-data';
import { useAuthStore } from '@/common/store/auth-store';
import { USER_FEATURE, userPaths, useRefreshUserManagement } from '../../user-management';
import { UnitChecklist } from '../components/UnitChecklist';

type Values = UserProfileValues & {
  employeeId?: number | null;
  password?: string;
  confirmPassword?: string;
  businessUnitIds?: string[];
};

export function UserFormPage() {
  const { t } = useTranslation();
  const { id } = useParams();
  const access = useFeatureAccess(USER_FEATURE);
  const draft = useFormDraft<Values>(USER_FEATURE, access.create && !id);
  const allowed = id ? access.edit : access.create;
  const source = useGetUser(id ?? '', {
    query: { queryKey: useUnitKey(getGetUserQueryKey(id ?? '')), enabled: allowed && !!id },
  });

  if (!allowed) return <ForbiddenResult />;

  const loading = (!!id && source.isPending) || draft.loading;

  return (
    <PageState
      loading={loading}
      missing={!loading && !!id && !source.data}
      missingTitle={t('user.notFound')}
      missingActions={[{ key: 'back', label: t('user.backToList'), to: userPaths.list }]}
    >
      <UserForm key={id ?? 'new'} user={id ? source.data : undefined} draft={draft} />
    </PageState>
  );
}

function UserForm({ user, draft }: { user?: UserDetail; draft: DraftState<Values> }) {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [params] = useSearchParams();
  const [form] = Form.useForm<Values>();
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const employees = useGetEmployeeOptions({
    query: { queryKey: useUnitKey(getGetEmployeeOptionsQueryKey()) },
  });
  const create = useCreateUser();
  const update = useUpdateUser();
  const refresh = useRefreshUserManagement();
  const goBack = useGoBack(userPaths.list);
  const forEmployee = idParam(params.get('employee'));

  const initialValues = useMemo<Partial<Values>>(
    () =>
      user
        ? { ...user.user, employeeId: user.user.employeeId }
        : { employeeId: forEmployee, businessUnitIds: unitId ? [unitId] : [], ...draft.values },
    [user, forEmployee, unitId, draft.values],
  );

  const fillFromEmployee = (employeeId?: number | null) =>
    employeeId
      ? getEmployee(employeeId).then((employee) => {
          const current = form.getFieldsValue();
          form.setFieldsValue({
            fullName: current.fullName || employee.name,
            email: current.email || employee.email || '',
            phone: current.phone || employee.mobile,
          });
        })
      : Promise.resolve();

  useEffect(() => {
    if (user || !forEmployee) return;
    void getEmployee(forEmployee).then((employee) =>
      form.setFieldsValue({
        fullName: employee.name,
        email: employee.email ?? '',
        phone: employee.mobile,
      }),
    );
  }, [user, forEmployee, form]);

  const submit = async (values: Values, action: string) => {
    const profile = {
      fullName: values.fullName.trim(),
      username: values.username.trim(),
      email: values.email.trim(),
      phone: values.phone?.trim() || null,
      employeeId: values.employeeId ?? null,
    };
    try {
      if (user) {
        await update.mutateAsync({ id: user.user.id, data: profile });
      } else {
        await create.mutateAsync({
          data: {
            ...profile,
            password: values.password ?? '',
            businessUnitIds: values.businessUnitIds ?? [],
          },
        });
      }
      const fromDraft = draft.draft.continuing;
      await draft.finish();
      await refresh();
      void message.success(t('user.saved', { name: profile.fullName }));
      if (action === 'new') {
        if (fromDraft) void navigate(userPaths.create);
        else form.resetFields();
      } else goBack();
    } catch (error) {
      void message.error(
        applyFieldErrors(form, error)
          ? t('master.fixFields')
          : problemMessage(error, t('user.failed')),
      );
      throw error;
    }
  };

  const employeeCard = (
    <DetailSection title={t('user.form.employee')} description={t('user.form.employeeHint')}>
      <Form.Item name="employeeId" label={t('user.col.employee')}>
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder={t('user.form.noEmployee')}
          onChange={(value: number | undefined) => void fillFromEmployee(value)}
          options={(employees.data ?? [])
            .filter((e) => e.isActive || e.id === user?.user.employeeId)
            .map((e) => ({ value: e.id, label: lookupLabel(e) }))}
        />
      </Form.Item>
    </DetailSection>
  );

  const signInCard = (
    <DetailSection
      title={t('user.form.signIn')}
      description={user ? undefined : t('user.form.signInHint')}
    >
      <UserProfileFields />
      {!user && <NewPasswordFields name="password" label={t('user.form.password')} />}
    </DetailSection>
  );

  const unitsCard = user ? (
    <DetailSection title={t('user.units.label')}>
      <DescriptionList
        columns={2}
        items={user.units.map((unit) => ({
          key: unit.businessUnitId,
          label: unit.businessUnitName,
          value: (
            <StatusTag tone={unit.isActive ? (unit.roleName ? 'success' : 'warning') : 'neutral'}>
              {unit.isActive ? (unit.roleName ?? t('user.noRole')) : t('user.inactive')}
            </StatusTag>
          ),
        }))}
      />
    </DetailSection>
  ) : (
    <DetailSection title={t('user.units.label')}>
      <UnitChecklist />
    </DetailSection>
  );

  const afterCard = (
    <DetailSection title={t('user.form.after')}>
      <ol>
        <li>{t('user.form.after1')}</li>
        <li>{t('user.form.after2')}</li>
        <li>{t('user.form.after3')}</li>
      </ol>
    </DetailSection>
  );

  return (
    <FormPageLayout<Values>
      form={form}
      initialValues={initialValues}
      title={user ? t('user.form.editTitle') : t('user.form.newTitle')}
      backTo={userPaths.list}
      crumbs={[user ? user.user.username : t('user.form.newTitle')]}
      cancelTo={userPaths.list}
      saving={create.isPending || update.isPending}
      onSubmit={submit}
      draft={user ? undefined : draft.draft}
      submitActions={[
        { key: 'new', label: t('user.form.saveNew'), hidden: !!user },
        { key: 'save', label: t('user.form.save'), icon: <CheckOutlined />, variant: 'primary' },
      ]}
      sections={[
        { key: 'employee', fields: ['employeeId'], content: employeeCard },
        {
          key: 'sign-in',
          fields: ['fullName', 'username', 'email', 'phone', 'password', 'confirmPassword'],
          content: signInCard,
        },
      ]}
      aside={[
        { key: 'units', fields: ['businessUnitIds'], content: unitsCard },
        ...(user ? [] : [{ key: 'after', content: afterCard }]),
      ]}
    />
  );
}
