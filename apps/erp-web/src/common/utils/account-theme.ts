import type { AccountTheme } from '@erp/api-client';
import {
  CUSTOM_PRESET,
  DEFAULT_PRESET,
  siderStyles,
  themePresets,
  useThemeStore,
  type ModePreference,
  type SiderStyle,
} from '@erp/ui';

const MODES: ModePreference[] = ['light', 'dark', 'system'];

export function applyAccountTheme(theme: AccountTheme) {
  const known = theme.preset === CUSTOM_PRESET || themePresets.some((p) => p.key === theme.preset);
  useThemeStore.setState({
    preset: known ? theme.preset : DEFAULT_PRESET,
    customPrimary: theme.customPrimary ?? null,
    sider: siderStyles.includes(theme.sider as SiderStyle) ? (theme.sider as SiderStyle) : 'light',
    mode: MODES.includes(theme.mode as ModePreference) ? (theme.mode as ModePreference) : 'light',
    compact: theme.compact,
  });
}
