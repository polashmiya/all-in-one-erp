import { EyeOutlined, LayoutOutlined } from '@ant-design/icons';
import {
  ApiError,
  getGetFeatureConfigurationQueryKey,
  useArrangeFeatureLayout,
  useGetFeatureConfiguration,
  useResetFeatureLayout,
  useResetSectionSetting,
  type FeatureConfigEditor,
  type SectionConfigRow,
} from '@erp/api-client';
import { DetailsPageLayout, EmptyState, FilterSelect } from '@erp/ui';
import { keepPreviousData, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router';
import { useAccountScreens } from '../../components/account-screens';
import { ACCOUNT_PATHS, SCREEN_PARAMS } from '../../constants';
import { LayoutBoard } from '../components/LayoutBoard';
import { LayoutPreview } from '../components/LayoutPreview';
import { LayoutScope } from '../components/LayoutScope';
import { SectionSettingsModal } from '../components/SectionSettingsModal';
import { buildLayout, moveNode, toLayoutRequest, type TabNode } from '../layout-model';

export function AccountLayoutPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const accountId = useParams().id ?? '';
  const [search, setSearch] = useSearchParams();
  const queryClient = useQueryClient();
  const { account, features, units } = useAccountScreens(accountId);
  const arrange = useArrangeFeatureLayout();
  const reset = useResetSectionSetting();
  const resetLayout = useResetFeatureLayout();
  const [draft, setDraft] = useState<TabNode[]>();
  const [editing, setEditing] = useState<SectionConfigRow>();
  const [previewing, setPreviewing] = useState(false);

  const feature = search.get(SCREEN_PARAMS.feature) ?? features[0]?.value ?? '';
  const unitId = search.get(SCREEN_PARAMS.unit) ?? undefined;
  const params = unitId ? { businessUnitId: unitId } : undefined;
  const queryKey = getGetFeatureConfigurationQueryKey(accountId, feature, params);
  const editor = useGetFeatureConfiguration(accountId, feature, params, {
    query: { enabled: !!feature, retry: false, placeholderData: keepPreviousData },
  });
  // While another scope loads, keep its business-unit badges but never show the previous scope's board.
  const data = editor.isPlaceholderData ? undefined : editor.data;
  // Every scope of the screen shares which business units have their own layout, so refresh them all.
  const refresh = () =>
    queryClient.invalidateQueries({
      queryKey: getGetFeatureConfigurationQueryKey(accountId, feature),
    });

  const choose = (key: string, value: string | null) => {
    const next = new URLSearchParams(search);
    if (value) next.set(key, value);
    else next.delete(key);
    setSearch(next, { replace: true });
  };

  const screen = features.find((f) => f.value === feature)?.label ?? '';
  const unit = units.find((u) => u.value === unitId)?.label ?? '';
  const scope = unitId ? t('layout.scopeUnit', { unit }) : t('layout.scopeDefault');
  const tabs = draft ?? (data ? buildLayout(data) : []);

  const failed = (error: unknown) =>
    void message.error(
      error instanceof ApiError
        ? (Object.values(error.fieldErrors)[0]?.[0] ?? error.message)
        : t('layout.failed'),
    );

  const saved = async (next: FeatureConfigEditor) => {
    queryClient.setQueryData(queryKey, next);
    await refresh();
  };

  const move = async (activeId: string, overId: string) => {
    const next = moveNode(tabs, activeId, overId);
    if (!next) return;
    const adopting = !!unitId && !data?.ownLayout;
    setDraft(next);
    try {
      await saved(
        await arrange.mutateAsync({
          id: accountId,
          code: feature,
          data: toLayoutRequest(next),
          params,
        }),
      );
      if (adopting) void message.success(t('layout.adopted', { unit }));
    } catch (error) {
      failed(error);
    } finally {
      setDraft(undefined);
    }
  };

  const resetSection = async (row: SectionConfigRow) => {
    try {
      await reset.mutateAsync({ id: accountId, code: feature, key: row.key, params });
      await refresh();
      void message.success(t('layout.resetDone'));
    } catch (error) {
      failed(error);
    }
  };

  const resetScope = async () => {
    try {
      await saved(await resetLayout.mutateAsync({ id: accountId, code: feature, params }));
      void message.success(
        unitId ? t('layout.resetUnitDone', { unit }) : t('layout.resetDefaultDone'),
      );
    } catch (error) {
      failed(error);
    }
  };

  return (
    <>
      <DetailsPageLayout
        loading={account.isPending}
        crumbs={[t('layout.action')]}
        header={{
          backTo: `${ACCOUNT_PATHS.configuration(accountId)}?${search.toString()}`,
          icon: <LayoutOutlined />,
          title: t('layout.title', { account: account.data?.name ?? '' }),
          meta: [{ key: 'scope', label: scope }],
          actions: [
            {
              key: 'preview',
              label: t('layout.preview'),
              icon: <EyeOutlined />,
              variant: 'primary',
              disabled: !data,
              onClick: () => setPreviewing(true),
            },
          ],
        }}
      >
        <div className="acc-layout">
          <div className="acc-layout__bar">
            <FilterSelect
              label={t('config.screen')}
              options={features}
              value={feature}
              onChange={(value) => choose(SCREEN_PARAMS.feature, value)}
            />
            <span className="acc-layout__hint">{t('layout.hint')}</span>
          </div>
          {feature && (
            <LayoutScope
              units={units}
              unitId={unitId}
              ownLayout={!!data?.ownLayout}
              layoutUnitIds={editor.data?.layoutUnitIds ?? []}
              resetting={resetLayout.isPending}
              onChoose={(value) => choose(SCREEN_PARAMS.unit, value)}
              onReset={resetScope}
            />
          )}
          {tabs.length > 0 ? (
            <LayoutBoard
              tabs={tabs}
              showChanges={!unitId}
              onMove={move}
              onEdit={setEditing}
              onReset={resetSection}
            />
          ) : (
            !editor.isFetching && (
              <EmptyState title={t(feature ? 'config.empty' : 'config.noScreens')} />
            )
          )}
        </div>
      </DetailsPageLayout>
      <SectionSettingsModal
        key={editing?.key}
        accountId={accountId}
        feature={feature}
        businessUnitId={unitId}
        scope={scope}
        row={editing}
        onClose={() => setEditing(undefined)}
        onSaved={refresh}
      />
      <LayoutPreview
        accountId={accountId}
        feature={feature}
        businessUnitId={unitId}
        open={previewing}
        title={t('layout.previewTitle', { screen })}
        subtitle={scope}
        onClose={() => setPreviewing(false)}
      />
    </>
  );
}
