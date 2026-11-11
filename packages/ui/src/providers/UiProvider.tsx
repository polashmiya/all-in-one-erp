import { App as AntApp, ConfigProvider } from 'antd';
import bnBD from 'antd/es/locale/bn_BD';
import enUS from 'antd/es/locale/en_US';
import dayjs from 'dayjs';
import { useEffect, useMemo, type ReactNode } from 'react';
import type { RouteObject } from 'react-router';
import { useShallow } from 'zustand/react/shallow';
import { requiredMark } from '../components/fields/required-mark';
import { useLanguageStore } from '../i18n/i18n';
import { HomePathContext, RoutesContext } from '../layout/app-shell/ShellContext';
import {
  fontStacks,
  scaledFontSizes,
  scaledRadii,
  tableMetrics,
  type UiPrefs,
} from '../theme/appearance';
import { buildBrand, tableHeadColors, type Brand } from '../theme/brand';
import { BrandContext } from '../theme/brand-context';
import { tokenVariables } from '../theme/design-tokens';
import { resolvePreset } from '../theme/presets';
import { loadFont } from '../theme/fonts';
import { buildTheme } from '../theme/tokens';
import { useResolvedMode, useThemeStore, useUiPrefs } from '../theme/theme-store';

const antdLocales = { en: enUS, bn: bnBD } as const;

const px = (values: Record<string, number>, prefix: string) =>
  Object.fromEntries(Object.entries(values).map(([k, v]) => [`--erp-${prefix}-${k}`, `${v}px`]));

function cssVariables(brand: Brand, prefs: UiPrefs): Record<string, string> {
  const { sider } = brand;
  const head = tableHeadColors(brand, prefs.tableHeader);
  const table = tableMetrics[prefs.tableSize];
  return {
    ...tokenVariables(brand.mode),
    ...px(scaledFontSizes(prefs.fontScale), 'font'),
    ...px(scaledRadii(prefs.radius), 'radius'),
    '--erp-font-family': fontStacks[prefs.font],
    '--erp-table-head-bg': head.bg,
    '--erp-table-head-fg': head.fg,
    '--erp-table-head-line': head.line,
    '--erp-table-stripe': brand.stripeBg,
    '--erp-row-h': `${table.row}px`,
    '--erp-row-h-sm': `${table.rowSm}px`,
    '--erp-head-h': `${table.head}px`,
    '--erp-head-h-sm': `${table.headSm}px`,
    '--erp-primary': brand.primary,
    '--erp-primary-hover': brand.primaryHover,
    '--erp-primary-soft': brand.primarySoft,
    '--erp-primary-ink': brand.primaryInk,
    '--erp-frame': brand.frameBg,
    '--erp-panel': brand.layoutBg,
    '--erp-surface': brand.containerBg,
    '--erp-header-border': brand.headerBorder,
    '--erp-border': brand.border,
    '--erp-border-2': brand.borderSecondary,
    '--erp-divider': brand.divider,
    '--erp-text-strong': brand.textStrong,
    '--erp-text': brand.text,
    '--erp-text-2': brand.textSecondary,
    '--erp-text-muted': brand.textMuted,
    '--erp-track': brand.trackBg,
    '--erp-table-head': brand.tableHeaderBg,
    '--erp-row-hover': brand.rowHoverBg,
    '--erp-tint': brand.tintBg,
    '--erp-card-shadow': brand.cardShadow,
    '--erp-deep': brand.deep,
    '--erp-accent': brand.accent,
    '--erp-brand-light': brand.palette[3]!,
    '--erp-brand-mid': brand.palette[5]!,
    '--erp-scrollbar':
      brand.mode === 'light' ? 'rgba(100, 110, 125, 0.35)' : 'rgba(255, 255, 255, 0.18)',
    '--sb-bg': sider.bg,
    '--sb-div': sider.divider,
    '--sb-tx': sider.text,
    '--sb-strong': sider.strong,
    '--sb-muted': sider.muted,
    '--sb-icon': sider.icon,
    '--sb-hv': sider.hoverBg,
    '--sb-guide': sider.guide,
    '--sb-input': sider.inputBg,
    '--sb-input-sh': sider.inputShadow,
    '--sb-kbd': sider.kbd,
    '--sb-act-bg': sider.activeBg,
    '--sb-act-tx': sider.activeText,
    '--sb-act-icon': sider.activeIcon,
    '--sb-act-sh': sider.activeShadow,
    '--sb-bar': sider.bar,
    '--sb-badge-bg': sider.badgeBg,
    '--sb-badge-tx': sider.badgeText,
    '--sb-panel-bd': sider.panelBorder,
  };
}

function DocumentThemeSync({ brand, prefs }: { brand: Brand; prefs: UiPrefs }) {
  useEffect(() => {
    const root = document.documentElement;
    root.dataset.theme = brand.mode;
    root.dataset.font = prefs.font;
    loadFont(prefs.font);
    root.dataset.tableHeader = prefs.tableHeader;
    root.dataset.tableBorder = prefs.tableBorder;
    root.dataset.tableStripe = String(prefs.tableStripe);
    root.dataset.contentWidth = prefs.contentWidth;
    root.dataset.motion = String(prefs.motion);
    root.style.colorScheme = brand.mode;
    for (const [name, value] of Object.entries(cssVariables(brand, prefs)))
      root.style.setProperty(name, value);
    document.body.style.background = brand.frameBg;
    document.body.style.color = brand.text;
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', brand.primary);
  }, [brand, prefs]);

  return null;
}

export function UiProvider({
  children,
  homePath = '/dashboard',
  routes = [],
}: {
  children: ReactNode;
  homePath?: string;
  routes?: RouteObject[];
}) {
  const { preset, customPrimary, sider, compact } = useThemeStore(
    useShallow((s) => ({
      preset: s.preset,
      customPrimary: s.customPrimary,
      sider: s.sider,
      compact: s.compact,
    })),
  );
  const mode = useResolvedMode();
  const prefs = useUiPrefs();
  const language = useLanguageStore((s) => s.language);

  const brand = useMemo(
    () => buildBrand(resolvePreset(preset, customPrimary), mode, sider),
    [preset, customPrimary, mode, sider],
  );
  const themeConfig = useMemo(
    () => ({ ...buildTheme({ mode, compact, brand, prefs }), hashed: false }),
    [mode, compact, brand, prefs],
  );

  useEffect(() => {
    dayjs.locale(language);
  }, [language]);

  return (
    <ConfigProvider theme={themeConfig} locale={antdLocales[language]} form={{ requiredMark }}>
      <AntApp>
        <BrandContext.Provider value={brand}>
          <DocumentThemeSync brand={brand} prefs={prefs} />
          <HomePathContext.Provider value={homePath}>
            <RoutesContext.Provider value={routes}>{children}</RoutesContext.Provider>
          </HomePathContext.Provider>
        </BrandContext.Provider>
      </AntApp>
    </ConfigProvider>
  );
}
