import { PlusOutlined, SaveOutlined } from '@ant-design/icons';
import {
  applyFieldErrors,
  getGetPermissionTreeQueryKey,
  getGetRoleQueryKey,
  problemMessage,
  useCreateRole,
  useGetPermissionTree,
  useGetRole,
  useUpdateRole,
} from '@erp/api-client';
import {
  DetailSection,
  FieldSection,
  FormPageLayout,
  PageState,
  SwitchList,
  usePlaceholder,
} from '@erp/ui';
import { App, Form, Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useUnitKey } from '@/common/master-data';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { USER_FEATURE, USER_ROLE_FEATURE } from '../../user-management';
import { PermissionTree } from '../components/PermissionTree';
import { RoleUsers } from '../components/RoleUsers';
import { ROLE_DESCRIPTION_MAX, ROLE_NAME_MAX, rolePaths } from '../constants';

interface RoleFormValues {
  name: string;
  description?: string | null;
  isActive: boolean;
  permissions: string[];
}

const EMPTY: RoleFormValues = { name: '', description: null, isActive: true, permissions: [] };

export function RoleFormPage() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const id = useParams().id;
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(USER_ROLE_FEATURE);
  const userAccess = useFeatureAccess(USER_FEATURE);
  const [form] = Form.useForm<RoleFormValues>();
  const role = useGetRole(id ?? '', {
    query: { enabled: !!id, queryKey: useUnitKey(getGetRoleQueryKey(id ?? '')) },
  });
  const tree = useGetPermissionTree({
    query: { queryKey: useUnitKey(getGetPermissionTreeQueryKey()) },
  });
  const create = useCreateRole();
  const update = useUpdateRole();
  const detail = role.data;
  const draft = useFormDraft<RoleFormValues>(USER_ROLE_FEATURE, !id && access.create);
  const readOnly = !!detail?.isAdministrator || !(id ? access.edit : access.create);

  if ((id && !detail) || draft.loading) {
    return (
      <PageState
        loading={role.isPending || draft.loading}
        missing={!draft.loading && !role.isPending}
        missingTitle={t('role.notFound')}
        missingActions={[{ key: 'back', label: t('role.backToList'), to: rolePaths.list }]}
      >
        {null}
      </PageState>
    );
  }

  const submit = async (values: RoleFormValues, action: string) => {
    try {
      const data = { ...values, description: values.description || null };
      const fromDraft = draft.draft.continuing;
      const saved = detail
        ? await update.mutateAsync({ id: detail.id, data })
        : await create.mutateAsync({ data });
      await draft.finish();
      void message.success(t('role.saved', { name: saved.name }));
      if (action === 'new') {
        if (detail || fromDraft) void navigate(rolePaths.create);
        else form.resetFields();
      } else void navigate(rolePaths.list);
    } catch (error) {
      void message.error(
        applyFieldErrors(form, error)
          ? t('master.fixFields')
          : problemMessage(error, t('role.failed')),
      );
      throw error;
    }
  };

  return (
    <FormPageLayout<RoleFormValues>
      form={form}
      initialValues={detail ? { ...detail } : { ...EMPTY, ...draft.values }}
      title={detail ? detail.name : t('role.newTitle')}
      subTitle={detail?.isAdministrator ? t('role.administratorNote') : t('role.formLead')}
      status={
        detail
          ? detail.isActive
            ? { tone: 'success', label: t('role.active') }
            : { tone: 'neutral', label: t('role.inactive') }
          : undefined
      }
      backTo={rolePaths.list}
      crumbs={[detail ? detail.name : t('role.newTitle')]}
      cancelTo={rolePaths.list}
      saving={create.isPending || update.isPending}
      onSubmit={submit}
      draft={detail || readOnly ? undefined : draft.draft}
      submitActions={[
        {
          key: 'new',
          label: t('role.saveNew'),
          icon: <PlusOutlined />,
          description: t('common.saveNewHint'),
          hidden: readOnly,
        },
        {
          key: 'save',
          label: t('role.save'),
          icon: <SaveOutlined />,
          variant: 'primary',
          hidden: readOnly,
        },
      ]}
      sections={[
        {
          key: 'role',
          fields: ['name', 'description'],
          content: (
            <FieldSection title={t('role.section.role')}>
              <Form.Item
                name="name"
                label={t('role.field.name')}
                rules={[{ required: true, whitespace: true }, { max: ROLE_NAME_MAX }]}
              >
                <Input
                  placeholder={placeholder('enter', t('role.field.name'))}
                  disabled={readOnly || detail?.isSystem}
                />
              </Form.Item>
              <Form.Item
                name="description"
                label={t('role.field.description')}
                rules={[{ max: ROLE_DESCRIPTION_MAX }]}
                className="erp-field-span-2"
              >
                <Input
                  placeholder={placeholder('enter', t('role.field.description'))}
                  disabled={readOnly}
                />
              </Form.Item>
            </FieldSection>
          ),
        },
        {
          key: 'permissions',
          fields: ['permissions'],
          content: (
            <DetailSection
              title={t('role.section.permissions')}
              description={t('role.permissionsLead', { unit: unit?.name ?? '' })}
            >
              <Form.Item name="permissions" noStyle>
                <PermissionTree
                  modules={tree.data ?? []}
                  loading={tree.isPending}
                  readOnly={readOnly}
                />
              </Form.Item>
            </DetailSection>
          ),
        },
      ]}
      aside={[
        {
          key: 'status',
          fields: ['isActive'],
          content: (
            <DetailSection title={t('role.section.status')} description={t('role.statusNote')}>
              <SwitchList options={[{ name: 'isActive', label: t('role.active') }]} />
            </DetailSection>
          ),
        },
        ...(detail
          ? [
              {
                key: 'users',
                content: (
                  <RoleUsers
                    role={detail}
                    readOnly={!access.edit && !userAccess.edit}
                    onChanged={() => role.refetch()}
                  />
                ),
              },
            ]
          : []),
      ]}
    />
  );
}
