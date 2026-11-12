import type { AccountTheme } from '@erp/api-client';
import { buildBrand, resolvePreset, type ThemeMode } from '@erp/ui';
import type { CSSProperties } from 'react';
import { useTranslation } from 'react-i18next';

const PREVIEW_ROWS = 3;

export function ThemePreview({ theme }: { theme: AccountTheme }) {
  const { t } = useTranslation();
  const mode: ThemeMode = theme.mode === 'dark' ? 'dark' : 'light';
  const brand = buildBrand(
    resolvePreset(theme.preset, theme.customPrimary),
    mode,
    theme.sider as Parameters<typeof buildBrand>[2],
  );
  const style = {
    '--pv-frame': brand.frameBg,
    '--pv-sider': brand.sider.bg,
    '--pv-sider-text': brand.sider.text,
    '--pv-sider-active': brand.sider.hoverBg,
    '--pv-surface': brand.containerBg,
    '--pv-border': brand.border,
    '--pv-head': brand.tableHeaderBg,
    '--pv-text': brand.text,
    '--pv-muted': brand.textMuted,
    '--pv-primary': brand.primary,
    '--pv-primary-soft': brand.primarySoft,
  } as CSSProperties;

  return (
    <div className={theme.compact ? 'acc-preview is-compact' : 'acc-preview'} style={style}>
      <aside className="acc-preview__sider">
        <span className="acc-preview__logo" />
        <span className="acc-preview__nav is-active">{t('theme.preview.menu')}</span>
        <span className="acc-preview__nav">{t('theme.preview.menu')}</span>
        <span className="acc-preview__nav">{t('theme.preview.menu')}</span>
      </aside>
      <main className="acc-preview__main">
        <div className="acc-preview__bar">
          <span className="acc-preview__title">{t('theme.preview.title')}</span>
          <span className="acc-preview__button">{t('theme.preview.button')}</span>
        </div>
        <div className="acc-preview__card">
          <div className="acc-preview__row is-head" />
          {Array.from({ length: PREVIEW_ROWS }, (_, i) => (
            <div key={i} className="acc-preview__row">
              <span className="acc-preview__tag" />
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}
