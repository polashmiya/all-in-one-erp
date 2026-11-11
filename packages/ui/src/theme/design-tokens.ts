import type { ThemeMode } from './brand';

export type Tone = 'success' | 'warning' | 'danger' | 'neutral' | 'info' | 'violet' | 'primary';
export type Tint = 'primary' | 'blue' | 'amber' | 'violet' | 'green' | 'slate';

export interface ToneColors {
  fg: string;
  bg: string;
  border: string;
  dot: string;
}

export const sizes = {
  siderWidth: 268,
  railWidth: 64,
  drawerWidth: 280,
  topbarHeight: 56,
  panelGutter: 8,
  panelRadius: 14,
  asideWidth: 320,
  searchWidth: 280,
  filterMaxWidth: 260,
  authFormWidth: 400,
  modal: { sm: 420, md: 560, lg: 760 },
  drawer: { sm: 380, md: 520, lg: 720 },
} as const;

export const space = {
  xxs: 2,
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
  huge: 48,
} as const;

export const radius = { xs: 5, sm: 6, md: 8, lg: 12, xl: 14, xxl: 20, pill: 999 } as const;

export const fontSize = {
  xs: 11,
  sm: 12,
  md: 13,
  base: 14,
  lg: 15,
  h2: 20,
  kpi: 24,
  display: 30,
  hero: 36,
} as const;

export const controlHeight = { xs: 22, sm: 30, md: 34, input: 38, lg: 46, xl: 48 } as const;

export const breakpoints = { sm: 576, md: 768, lg: 992, xl: 1200 } as const;

export const fontFamily =
  "'Inter Variable', 'Inter', 'Hind Siliguri', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

export const shadows = {
  light: {
    card: '0 1px 2px rgba(15, 23, 42, 0.04)',
    panel: '0 1px 3px rgba(15, 23, 42, 0.06)',
    popover: '0 16px 40px rgba(15, 23, 42, 0.16)',
    raised: '0 30px 60px rgba(0, 0, 0, 0.28)',
  },
  dark: {
    card: 'none',
    panel: 'none',
    popover: '0 16px 40px rgba(0, 0, 0, 0.5)',
    raised: '0 30px 60px rgba(0, 0, 0, 0.5)',
  },
} as const;

export const surfaces: Record<ThemeMode, Record<string, string>> = {
  light: {
    panel: '#F7F8FA',
    surface: '#FFFFFF',
    headerBorder: '#ECEEF1',
    border: '#D5DAE3',
    borderSecondary: '#E8EAEE',
    borderStrong: '#B8C0CC',
    borderHover: '#9AA6B8',
    divider: '#F0F1F4',
    tableHead: '#FAFAFB',
    rowHover: '#F8FAFF',
    track: '#EEF0F4',
    textStrong: '#0E1320',
    text: '#1F2937',
    textSecondary: '#4B5563',
    textMuted: '#5F6B7A',
    placeholder: '#8A94A3',
    chartBar: '#E3E6EB',
    chartBarHover: '#CFD4DC',
    tooltipBg: '#0E1320',
    tooltipText: '#FFFFFF',
    onPrimary: '#FFFFFF',
    required: '#DC2626',
    online: '#22C55E',
    favorite: '#D97706',
    neutralDot: '#C3C8D0',
    scrollbar: 'rgba(100, 110, 125, 0.35)',
    rowHoverOverlay: 'rgba(15, 23, 42, 0.05)',
  },
  dark: {
    panel: '#10141A',
    surface: '#161B22',
    headerBorder: 'rgba(255, 255, 255, 0.08)',
    border: '#3A414D',
    borderSecondary: 'rgba(255, 255, 255, 0.08)',
    borderStrong: '#4B5361',
    borderHover: '#6B7280',
    divider: 'rgba(255, 255, 255, 0.06)',
    tableHead: '#1B212A',
    rowHover: 'rgba(255, 255, 255, 0.03)',
    track: 'rgba(255, 255, 255, 0.06)',
    textStrong: '#F3F4F6',
    text: '#D1D5DB',
    textSecondary: '#A3AAB6',
    textMuted: '#8B93A1',
    placeholder: '#6B7280',
    chartBar: 'rgba(255, 255, 255, 0.14)',
    chartBarHover: 'rgba(255, 255, 255, 0.24)',
    tooltipBg: '#F3F4F6',
    tooltipText: '#0E1320',
    onPrimary: '#FFFFFF',
    required: '#F87171',
    online: '#22C55E',
    favorite: '#FBBF24',
    neutralDot: '#4B5361',
    scrollbar: 'rgba(255, 255, 255, 0.18)',
    rowHoverOverlay: 'rgba(255, 255, 255, 0.06)',
  },
};

export const onBrand = {
  text: '#FFFFFF',
  muted: 'rgba(255, 255, 255, 0.78)',
  subtle: 'rgba(255, 255, 255, 0.72)',
  fill: 'rgba(255, 255, 255, 0.12)',
  highlight: '#4ADE80',
  glass: 'rgba(255, 255, 255, 0.08)',
  glassBorder: 'rgba(255, 255, 255, 0.16)',
  dots: 'rgba(255, 255, 255, 0.07)',
} as const;

export const tones: Record<ThemeMode, Record<Exclude<Tone, 'primary'>, ToneColors>> = {
  light: {
    success: { fg: '#15803D', bg: '#ECFDF3', border: '#BBF7D0', dot: '#16A34A' },
    warning: { fg: '#B45309', bg: '#FFFBEB', border: '#FDE68A', dot: '#F59E0B' },
    danger: { fg: '#B91C1C', bg: '#FEF2F2', border: '#FECACA', dot: '#EF4444' },
    neutral: { fg: '#475569', bg: '#F1F5F9', border: '#E2E8F0', dot: '#94A3B8' },
    info: { fg: '#1D4ED8', bg: '#EFF6FF', border: '#BFDBFE', dot: '#3B82F6' },
    violet: { fg: '#6D28D9', bg: '#F5F3FF', border: '#DDD6FE', dot: '#8B5CF6' },
  },
  dark: {
    success: {
      fg: '#4ADE80',
      bg: 'rgba(34, 197, 94, 0.12)',
      border: 'rgba(34, 197, 94, 0.25)',
      dot: '#22C55E',
    },
    warning: {
      fg: '#FBBF24',
      bg: 'rgba(245, 158, 11, 0.12)',
      border: 'rgba(245, 158, 11, 0.25)',
      dot: '#F59E0B',
    },
    danger: {
      fg: '#F87171',
      bg: 'rgba(239, 68, 68, 0.12)',
      border: 'rgba(239, 68, 68, 0.25)',
      dot: '#EF4444',
    },
    neutral: {
      fg: '#CBD5E1',
      bg: 'rgba(148, 163, 184, 0.12)',
      border: 'rgba(148, 163, 184, 0.22)',
      dot: '#94A3B8',
    },
    info: {
      fg: '#93C5FD',
      bg: 'rgba(59, 130, 246, 0.14)',
      border: 'rgba(59, 130, 246, 0.25)',
      dot: '#3B82F6',
    },
    violet: {
      fg: '#C4B5FD',
      bg: 'rgba(139, 92, 246, 0.14)',
      border: 'rgba(139, 92, 246, 0.25)',
      dot: '#8B5CF6',
    },
  },
};

export const tints: Record<
  ThemeMode,
  Record<Exclude<Tint, 'primary'>, { fg: string; bg: string }>
> = {
  light: {
    blue: { fg: '#1D4ED8', bg: '#DBEAFE' },
    amber: { fg: '#B45309', bg: '#FEF3C7' },
    violet: { fg: '#6D28D9', bg: '#EDE9FE' },
    green: { fg: '#15803D', bg: '#ECFDF3' },
    slate: { fg: '#475569', bg: '#F1F5F9' },
  },
  dark: {
    blue: { fg: '#93C5FD', bg: 'rgba(59, 130, 246, 0.16)' },
    amber: { fg: '#FBBF24', bg: 'rgba(245, 158, 11, 0.16)' },
    violet: { fg: '#C4B5FD', bg: 'rgba(139, 92, 246, 0.16)' },
    green: { fg: '#4ADE80', bg: 'rgba(34, 197, 94, 0.14)' },
    slate: { fg: '#CBD5E1', bg: 'rgba(148, 163, 184, 0.14)' },
  },
};

export const tintOrder: Tint[] = ['primary', 'blue', 'violet', 'amber', 'green', 'slate'];

export const sidebarPalettes = {
  light: {
    bg: '#F2F3F5',
    divider: '#E3E5E9',
    text: '#3D4452',
    strong: '#0E1320',
    muted: '#626A77',
    icon: '#6B7280',
    guide: '#D7DAE0',
    kbd: '#E3E5E9',
    activeBg: '#FFFFFF',
    badgeBg: '#E6E8EC',
  },
  dark: { bg: '#0B0F17', text: '#C9CED6', muted: '#8B93A1', guide: '#252B36' },
  darkMode: { bg: '#0E1218' },
} as const;

const kebab = (key: string) => key.replace(/[A-Z]/g, (c) => `-${c.toLowerCase()}`);

export function tokenVariables(mode: ThemeMode): Record<string, string> {
  const vars: Record<string, string> = {};
  for (const [key, value] of Object.entries(surfaces[mode])) vars[`--erp-${kebab(key)}`] = value;
  for (const [tone, colors] of Object.entries(tones[mode])) {
    for (const [part, value] of Object.entries(colors)) vars[`--erp-tone-${tone}-${part}`] = value;
  }
  for (const [tint, colors] of Object.entries(tints[mode])) {
    vars[`--erp-tint-${tint}-fg`] = colors.fg;
    vars[`--erp-tint-${tint}-bg`] = colors.bg;
  }
  for (const [key, value] of Object.entries(onBrand)) vars[`--erp-on-brand-${kebab(key)}`] = value;
  for (const [key, value] of Object.entries(shadows[mode])) vars[`--erp-shadow-${key}`] = value;
  for (const [key, value] of Object.entries(radius)) vars[`--erp-radius-${key}`] = `${value}px`;
  for (const [key, value] of Object.entries(space)) vars[`--erp-space-${key}`] = `${value}px`;
  for (const [key, value] of Object.entries(fontSize)) vars[`--erp-font-${key}`] = `${value}px`;
  for (const [key, value] of Object.entries(controlHeight))
    vars[`--erp-control-${key}`] = `${value}px`;
  vars['--erp-sider-w'] = `${sizes.siderWidth}px`;
  vars['--erp-rail-w'] = `${sizes.railWidth}px`;
  vars['--erp-topbar-h'] = `${sizes.topbarHeight}px`;
  vars['--erp-gutter'] = `${sizes.panelGutter}px`;
  vars['--erp-panel-radius'] = `${sizes.panelRadius}px`;
  vars['--erp-aside-w'] = `${sizes.asideWidth}px`;
  vars['--erp-search-w'] = `${sizes.searchWidth}px`;
  vars['--erp-filter-max-w'] = `${sizes.filterMaxWidth}px`;
  vars['--erp-auth-w'] = `${sizes.authFormWidth}px`;
  vars['--erp-font-family'] = fontFamily;
  return vars;
}
