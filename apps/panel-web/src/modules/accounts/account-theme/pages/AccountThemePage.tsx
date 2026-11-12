import { CheckOutlined, UndoOutlined } from '@ant-design/icons';
import {
  ApiError,
  getGetAccountQueryKey,
  useGetAccount,
  useResetAccountTheme,
  useSaveAccountTheme,
  type AccountDetail,
  type AccountTheme,
} from '@erp/api-client';
import {
  CUSTOM_PRESET,
  DEFAULT_PRESET,
  Field,
  FieldGrid,
  findPreset,
  FormPageLayout,
  PageState,
  siderStyles,
  themePresets,
  useLanguageStore,
  YesNoSwitch,
} from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { App, ColorPicker, Form, Segmented } from 'antd';
import type { CSSProperties } from 'react';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { FormSection } from '../../account-setup/components/FormSection';
import { ACCOUNT_PATHS } from '../../constants';
import { ThemePreview } from '../components/ThemePreview';

const MODES = ['light', 'dark', 'system'] as const;

const DEFAULT_THEME: AccountTheme = {
  preset: DEFAULT_PRESET,
  customPrimary: null,
  sider: 'light',
  mode: 'light',
  compact: false,
};

export function AccountThemePage() {
  const { t } = useTranslation();
  const id = useParams().id ?? '';
  const query = useGetAccount(id);

  return (
    <PageState
      loading={query.isPending}
      missing={!query.isPending && !query.data}
      missingTitle={t('detail.loadFailed')}
      missingActions={[{ key: 'back', label: t('accounts.title'), to: ACCOUNT_PATHS.list }]}
    >
      {query.data && <ThemeForm account={query.data} />}
    </PageState>
  );
}

function PresetPicker({ value, onChange }: { value?: string; onChange?: (value: string) => void }) {
  const language = useLanguageStore((s) => s.language);
  return (
    <div className="erp-themepanel__swatches acc-theme__swatches">
      {themePresets.map((preset) => (
        <button
          key={preset.key}
          type="button"
          className="erp-swatch"
          style={{ '--swatch': preset.primary } as CSSProperties}
          aria-pressed={value === preset.key}
          onClick={() => onChange?.(preset.key)}
        >
          <span className="erp-swatch__dot">{value === preset.key && <CheckOutlined />}</span>
          {preset.name[language] ?? preset.name.en}
        </button>
      ))}
    </div>
  );
}

function ThemeForm({ account }: { account: AccountDetail }) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [form] = Form.useForm<AccountTheme>();
  const save = useSaveAccountTheme();
  const reset = useResetAccountTheme();
  const watched = Form.useWatch([], form) as AccountTheme | undefined;
  const preview = { ...DEFAULT_THEME, ...account.theme, ...watched };
  const initialValues = useMemo(() => {
    const theme = { ...DEFAULT_THEME, ...account.theme };
    return { ...theme, customPrimary: theme.customPrimary ?? findPreset(theme.preset).primary };
  }, [account.theme]);
  const detailPath = ACCOUNT_PATHS.detail(account.id);
  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: getGetAccountQueryKey(account.id) });

  const submit = async (values: AccountTheme, action: string) => {
    try {
      if (action === 'reset') {
        await reset.mutateAsync({ id: account.id });
        form.setFieldsValue(DEFAULT_THEME);
      } else {
        const custom = values.preset === CUSTOM_PRESET ? values.customPrimary : null;
        await save.mutateAsync({ id: account.id, data: { ...values, customPrimary: custom } });
      }
      await refresh();
      void message.success(t(action === 'reset' ? 'theme.resetDone' : 'theme.saved'));
    } catch (error) {
      void message.error(
        error instanceof ApiError
          ? (Object.values(error.fieldErrors)[0]?.[0] ?? error.message)
          : t('theme.failed'),
      );
      throw error;
    }
  };

  return (
    <FormPageLayout<AccountTheme>
      form={form}
      initialValues={initialValues}
      title={t('theme.title', { account: account.name })}
      subTitle={t('theme.subtitle')}
      backTo={detailPath}
      cancelTo={detailPath}
      crumbs={[account.name, t('theme.action')]}
      saving={save.isPending || reset.isPending}
      onSubmit={submit}
      submitActions={[
        {
          key: 'reset',
          label: t('theme.reset'),
          icon: <UndoOutlined />,
          hidden: !account.theme,
          validate: [],
        },
        { key: 'save', label: t('theme.save'), icon: <CheckOutlined />, variant: 'primary' },
      ]}
    >
      <div className="acc-form acc-theme">
        <div className="acc-theme__form">
          <FormSection title={t('shell.primaryColor')} description={t('theme.colorHint')}>
            <Field name="preset">
              <PresetPicker />
            </Field>
            <Field
              name="customPrimary"
              label={t('shell.customColor')}
              getValueFromEvent={(color: { toHexString: () => string }) =>
                color.toHexString().toUpperCase()
              }
            >
              <ColorPicker
                showText
                onChangeComplete={() => form.setFieldValue('preset', CUSTOM_PRESET)}
              />
            </Field>
          </FormSection>
          <FormSection title={t('theme.layout')} description={t('theme.layoutHint')}>
            <FieldGrid columns={2}>
              <Field name="sider" label={t('shell.sidebarStyle')}>
                <Segmented
                  options={siderStyles.map((key) => ({
                    value: key,
                    label: t(`theme.sider.${key}`),
                  }))}
                />
              </Field>
              <Field name="mode" label={t('theme.mode')}>
                <Segmented
                  options={MODES.map((key) => ({ value: key, label: t(`theme.modes.${key}`) }))}
                />
              </Field>
              <Field name="compact" label={t('shell.compactMode')} valuePropName="checked">
                <YesNoSwitch />
              </Field>
            </FieldGrid>
          </FormSection>
        </div>
        <aside className="acc-theme__preview">
          <span className="acc-theme__preview-label">{t('theme.preview.label')}</span>
          <ThemePreview theme={preview} />
          <p className="acc-theme__note">{t('theme.note')}</p>
        </aside>
      </div>
    </FormPageLayout>
  );
}
