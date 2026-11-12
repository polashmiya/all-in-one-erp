import {
  CheckCircleOutlined,
  EditOutlined,
  PlusOutlined,
  StopOutlined,
  UndoOutlined,
} from '@ant-design/icons';
import {
  getGetFeatureConfigurationQueryKey,
  useGetAccount,
  useGetFeatureConfiguration,
  useResetFieldSetting,
  useSetCustomFieldStatus,
  type FieldConfigRow,
} from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { ListPageLayout, StatusTag, TitleCell, useListState, useUndoMessage } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { App, type TableColumnsType } from 'antd';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { appConfig } from '@/common/config/app.config';
import { useFeatureCatalog } from '../../components/feature-catalog';
import { CustomFieldModal } from '../components/CustomFieldModal';
import { FieldSettingsModal } from '../components/FieldSettingsModal';
import { effective } from '../field-settings';

type Filters = { feature: string | null; unit: string | null };

export function AccountConfigurationPage() {
  const { t } = useTranslation();
  const text = useText();
  const { message } = App.useApp();
  const accountId = useParams().id ?? '';
  const queryClient = useQueryClient();
  const account = useGetAccount(accountId);
  const { modules } = useFeatureCatalog();
  const reset = useResetFieldSetting();
  const setStatus = useSetCustomFieldStatus();
  const showUndo = useUndoMessage();
  const [editing, setEditing] = useState<FieldConfigRow>();
  const [adding, setAdding] = useState(false);
  const list = useListState<Filters>({
    filters: { feature: null, unit: null },
    pageSize: appConfig.defaultPageSize,
  });

  const granted = account.data?.featureCodes ?? [];
  const features = modules.flatMap((module) =>
    module.subModules.flatMap((subModule) =>
      subModule.features
        .filter((feature) => granted.includes(feature.code))
        .map((feature) => ({
          value: feature.code,
          label: `${text(subModule.label)} › ${text(feature.label)}`,
        })),
    ),
  );
  const units = (account.data?.businessUnits ?? []).map((unit) => ({
    value: unit.id,
    label: unit.name,
  }));

  const firstFeature = features[0]?.value;
  const { setFilter } = list;
  useEffect(() => {
    if (!list.filters.feature && firstFeature) setFilter('feature', firstFeature);
  }, [list.filters.feature, firstFeature, setFilter]);

  const feature = list.filters.feature ?? firstFeature ?? '';
  const unitId = list.filters.unit ?? undefined;
  const params = unitId ? { businessUnitId: unitId } : undefined;
  const queryKey = getGetFeatureConfigurationQueryKey(accountId, feature, params);
  const editor = useGetFeatureConfiguration(accountId, feature, params, {
    query: { enabled: !!feature, retry: false },
  });
  const refresh = () => queryClient.invalidateQueries({ queryKey });

  const scope = unitId
    ? t('config.scopeUnit', { unit: units.find((u) => u.value === unitId)?.label ?? '' })
    : t('config.scopeAccount');
  const sections = editor.data?.sections ?? [];
  const sectionLabel = (key?: string | null) => {
    const section = sections.find((s) => s.key === key);
    return section ? `${text(section.tabLabel)} › ${text(section.label)}` : '—';
  };
  const labelOf = (row: FieldConfigRow) => {
    const values = effective(row);
    return text({ en: values.labelEn ?? row.key, bn: values.labelBn ?? null }) ?? row.key;
  };

  const search = list.query.search.trim().toLowerCase();
  const rows = editor.data?.fields.filter(
    (row) =>
      !search ||
      row.key.toLowerCase().includes(search) ||
      labelOf(row).toLowerCase().includes(search),
  );

  const yesNo = (on?: boolean | null) => (
    <StatusTag tone={on ? 'success' : 'neutral'} dot>
      {on ? t('common.yes') : t('common.no')}
    </StatusTag>
  );
  const always = <StatusTag tone="info">{t('config.always')}</StatusTag>;

  const resetField = async (row: FieldConfigRow) => {
    try {
      await reset.mutateAsync({ id: accountId, code: feature, key: row.key, params });
      await refresh();
      void message.success(t('config.resetDone'));
    } catch {
      void message.error(t('config.failed'));
    }
  };

  const setActive = async (row: FieldConfigRow, isActive: boolean, undoable = true) => {
    try {
      await setStatus.mutateAsync({
        id: accountId,
        code: feature,
        key: row.key,
        data: { isActive },
      });
      await refresh();
      const done = t(isActive ? 'config.custom.activated' : 'config.custom.deactivated');
      if (undoable) showUndo(done, () => void setActive(row, !isActive, false));
      else void message.success(done);
    } catch {
      void message.error(t('config.failed'));
    }
  };

  const columns: TableColumnsType<FieldConfigRow> = [
    {
      key: 'field',
      title: t('config.col.field'),
      render: (_, row) => <TitleCell title={labelOf(row)} subtitle={row.key} />,
    },
    {
      key: 'section',
      title: t('config.col.section'),
      render: (_, row) => sectionLabel(effective(row).sectionKey),
    },
    {
      key: 'type',
      title: t('config.col.type'),
      render: (_, row) => (
        <>
          {t(`config.type.${row.type}`)}{' '}
          {row.isCustom && <StatusTag tone="info">{t('config.custom.tag')}</StatusTag>}
        </>
      ),
    },
    {
      key: 'shown',
      title: t('config.col.shown'),
      render: (_, row) => (row.isLocked ? always : yesNo(effective(row).isVisible)),
    },
    {
      key: 'required',
      title: t('config.col.required'),
      render: (_, row) => (row.isLocked ? always : yesNo(effective(row).isRequired)),
    },
    {
      key: 'list',
      title: t('config.col.list'),
      render: (_, row) => (row.canList ? yesNo(effective(row).inList) : '—'),
    },
    {
      key: 'filter',
      title: t('config.col.filter'),
      render: (_, row) => (row.canFilter ? yesNo(effective(row).inFilter) : '—'),
    },
    {
      key: 'setting',
      title: t('config.col.setting'),
      render: (_, row) =>
        !row.isActive ? (
          <StatusTag tone="neutral">{t('accounts.status.inactive')}</StatusTag>
        ) : row.override ? (
          <StatusTag tone="violet">{t('config.changed')}</StatusTag>
        ) : (
          <StatusTag>{t('config.default')}</StatusTag>
        ),
    },
  ];

  return (
    <>
      <ListPageLayout<FieldConfigRow, Filters, string>
        title={t('config.title', { account: account.data?.name ?? '' })}
        subtitle={t('config.subtitle')}
        actions={[
          {
            key: 'new',
            label: t('config.custom.new'),
            icon: <PlusOutlined />,
            variant: 'primary',
            disabled: !editor.data,
            onClick: () => setAdding(true),
          },
        ]}
        state={list}
        search={{ label: t('config.search'), placeholder: t('config.search') }}
        filters={[
          { key: 'feature', label: t('config.screen'), options: features },
          { key: 'unit', label: t('config.unit'), options: units },
        ]}
        columns={columns}
        rows={rows}
        rowKey="key"
        total={rows?.length}
        loading={account.isPending || editor.isFetching}
        onRefresh={() => void editor.refetch()}
        onRowClick={setEditing}
        rowActions={(row) => [
          {
            key: 'edit',
            label: t('config.edit'),
            icon: <EditOutlined />,
            onClick: () => setEditing(row),
          },
          {
            key: 'reset',
            label: t('config.reset'),
            icon: <UndoOutlined />,
            inMenu: true,
            hidden: !row.override,
            confirm: {
              title: t('config.resetConfirm', { field: labelOf(row) }),
              description: t('config.resetHint'),
              okText: t('config.reset'),
            },
            onClick: () => resetField(row),
          },
          {
            key: 'status',
            label: row.isActive ? t('lookups.deactivate') : t('lookups.activate'),
            icon: row.isActive ? <StopOutlined /> : <CheckCircleOutlined />,
            inMenu: true,
            hidden: !row.isCustom,
            onClick: () => setActive(row, !row.isActive),
          },
        ]}
        empty={{ title: t(feature ? 'config.empty' : 'config.noScreens') }}
      />
      <FieldSettingsModal
        key={editing?.key}
        accountId={accountId}
        feature={feature}
        businessUnitId={unitId}
        scope={scope}
        row={editing}
        sections={sections}
        onClose={() => setEditing(undefined)}
        onSaved={refresh}
      />
      <CustomFieldModal
        accountId={accountId}
        feature={feature}
        open={adding}
        types={editor.data?.customTypes ?? []}
        sections={sections}
        onClose={() => setAdding(false)}
        onSaved={refresh}
      />
    </>
  );
}
