import { SaveOutlined, UndoOutlined } from '@ant-design/icons';
import {
  getGetApprovalLevelsQueryKey,
  useGetAccount,
  useGetApprovalLevels,
  useListApprovalFeatures,
  useListApprovers,
  useSaveApprovalLevels,
  type ApprovalLevelSet,
} from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import {
  DetailSection,
  EmptyState,
  FilterSelect,
  FormPageLayout,
  PageState,
  StatusTag,
} from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { App, Form } from 'antd';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router';
import { ACCOUNT_PATHS, SCREEN_PARAMS } from '../../constants';
import { LevelsEditor, type LevelValue } from '../components/LevelsEditor';

type Values = { levels: LevelValue[] };

const toValues = (set?: ApprovalLevelSet): Values => ({
  levels: (set?.levels ?? []).map((level) => ({
    name: level.name,
    kind: level.userId ? 'user' : 'role',
    approverId: level.userId ?? level.roleId ?? undefined,
  })),
});

export function AccountApprovalsPage() {
  const { t } = useTranslation();
  const text = useText();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const accountId = useParams().id ?? '';
  const [search, setSearch] = useSearchParams();
  const [form] = Form.useForm<Values>();
  const account = useGetAccount(accountId);
  const features = useListApprovalFeatures(accountId);
  const approvers = useListApprovers(accountId);
  const save = useSaveApprovalLevels();

  const feature = search.get(SCREEN_PARAMS.feature) ?? features.data?.[0]?.code ?? '';
  const unit = search.get(SCREEN_PARAMS.unit) ?? undefined;
  const params = { feature, businessUnitId: unit };
  const levels = useGetApprovalLevels(accountId, params, { query: { enabled: !!feature } });

  const setScope = (key: string, value: string | null) => {
    const next = new URLSearchParams(search);
    if (value) next.set(key, value);
    else next.delete(key);
    setSearch(next, { replace: true });
  };

  const submit = async (values: Values, action: string) => {
    try {
      await save.mutateAsync({
        id: accountId,
        params,
        data: {
          levels:
            action === 'inherit'
              ? []
              : values.levels.map((level) => ({
                  name: level.name?.trim() ?? '',
                  roleId: level.kind === 'role' ? (level.approverId ?? null) : null,
                  userId: level.kind === 'user' ? (level.approverId ?? null) : null,
                })),
        },
      });
      await queryClient.invalidateQueries({
        queryKey: getGetApprovalLevelsQueryKey(accountId, params),
      });
      void message.success(t('approvals.saved'));
    } catch (error) {
      void message.error(t('approvals.failed'));
      throw error;
    }
  };

  const loading = account.isPending || features.isPending || (!!feature && levels.isPending);
  const own = !!unit && !!levels.data && !levels.data.inherited && levels.data.levels.length > 0;

  return (
    <PageState loading={loading} missing={!loading && !account.data}>
      {features.data?.length === 0 ? (
        <EmptyState title={t('approvals.noScreens')} />
      ) : (
        <FormPageLayout<Values>
          key={`${feature}:${unit ?? ''}:${levels.dataUpdatedAt}`}
          form={form}
          initialValues={toValues(levels.data)}
          title={t('approvals.title', { account: account.data?.name ?? '' })}
          subTitle={t('approvals.subtitle')}
          backTo={ACCOUNT_PATHS.detail(accountId)}
          crumbs={[account.data?.name ?? '', t('workspace.tool.approvals')]}
          saving={save.isPending}
          onSubmit={submit}
          submitActions={[
            {
              key: 'inherit',
              label: t('approvals.inherit'),
              icon: <UndoOutlined />,
              description: t('approvals.inheritHint'),
              validate: [],
              hidden: !own,
            },
            { key: 'save', label: t('approvals.save'), icon: <SaveOutlined />, variant: 'primary' },
          ]}
          sections={[
            {
              key: 'scope',
              content: (
                <DetailSection title={t('approvals.scope')} description={t('approvals.scopeHint')}>
                  <div className="acc-levels__scope">
                    <FilterSelect
                      label={t('approvals.screen')}
                      value={feature}
                      onChange={(value) => setScope(SCREEN_PARAMS.feature, value)}
                      options={(features.data ?? []).map((f) => ({
                        value: f.code,
                        label: text(f.label) ?? f.code,
                      }))}
                    />
                    <FilterSelect
                      label={t('approvals.unit')}
                      value={unit ?? null}
                      onChange={(value) => setScope(SCREEN_PARAMS.unit, value)}
                      options={(account.data?.businessUnits ?? []).map((u) => ({
                        value: u.id,
                        label: u.name,
                      }))}
                    />
                    {!unit && <StatusTag tone="info">{t('approvals.allUnits')}</StatusTag>}
                    {levels.data?.inherited && (
                      <StatusTag tone="warning">{t('approvals.inherited')}</StatusTag>
                    )}
                  </div>
                </DetailSection>
              ),
            },
            {
              key: 'levels',
              fields: ['levels'],
              content: <LevelsEditor approvers={approvers.data} />,
            },
          ]}
        />
      )}
    </PageState>
  );
}
