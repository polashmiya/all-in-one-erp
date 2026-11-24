import { useSyncExternalStore } from 'react';
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { useShallow } from 'zustand/react/shallow';
import { defaultUiPrefs, sanitizeUiPrefs, type UiPrefs } from './appearance';
import { siderStyles, type SiderStyle, type ThemeMode } from './brand';
import { CUSTOM_PRESET, DEFAULT_PRESET, themePresets } from './presets';

export type ModePreference = ThemeMode | 'system';

export interface Appearance extends UiPrefs {
  mode: ModePreference;
  preset: string;
  customPrimary: string | null;
  sider: SiderStyle;
  compact: boolean;
}

interface ThemeState extends Appearance {
  siderCollapsed: boolean;
  setMode: (mode: ModePreference) => void;
  setPreset: (preset: string) => void;
  setCustomPrimary: (color: string) => void;
  setSider: (sider: SiderStyle) => void;
  setCompact: (compact: boolean) => void;
  setPrefs: (prefs: Partial<UiPrefs>) => void;
  setSiderCollapsed: (collapsed: boolean) => void;
  resetAppearance: () => void;
}

export const defaultAppearance: Appearance = {
  mode: 'light',
  preset: DEFAULT_PRESET,
  customPrimary: null,
  sider: 'light',
  compact: false,
  ...defaultUiPrefs,
};

const modes: ModePreference[] = ['light', 'dark', 'system'];

export const useThemeStore = create<ThemeState>()(
  persist(
    (set) => ({
      ...defaultAppearance,
      siderCollapsed: false,
      setMode: (mode) => set({ mode }),
      setPreset: (preset) => set({ preset }),
      setCustomPrimary: (customPrimary) => set({ preset: CUSTOM_PRESET, customPrimary }),
      setSider: (sider) => set({ sider }),
      setCompact: (compact) => set({ compact }),
      setPrefs: (prefs) => set(prefs),
      setSiderCollapsed: (siderCollapsed) => set({ siderCollapsed }),
      resetAppearance: () => set(defaultAppearance),
    }),
    {
      name: 'erp.ui.preferences',
      version: 3,
      migrate: (persisted, version) => {
        const saved = (persisted ?? {}) as Partial<ThemeState>;
        const state = { ...defaultAppearance, ...saved, ...sanitizeUiPrefs(saved) };
        if (version < 3 && state.font === 'inter') state.font = defaultAppearance.font;
        if (!modes.includes(state.mode)) state.mode = defaultAppearance.mode;
        if (!siderStyles.includes(state.sider)) state.sider = 'light';
        if (state.preset !== CUSTOM_PRESET && !themePresets.some((p) => p.key === state.preset))
          state.preset = DEFAULT_PRESET;
        return state as ThemeState;
      },
    },
  ),
);

const DARK_QUERY = '(prefers-color-scheme: dark)';

function subscribeToSystemMode(onChange: () => void) {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function')
    return () => undefined;
  const query = window.matchMedia(DARK_QUERY);
  query.addEventListener('change', onChange);
  return () => query.removeEventListener('change', onChange);
}

function systemMode(): ThemeMode {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return 'light';
  return window.matchMedia(DARK_QUERY).matches ? 'dark' : 'light';
}

export function useResolvedMode(): ThemeMode {
  const preference = useThemeStore((s) => s.mode);
  const system = useSyncExternalStore(subscribeToSystemMode, systemMode, () => 'light' as const);
  return preference === 'system' ? system : preference;
}

export function useUiPrefs(): UiPrefs {
  return useThemeStore(
    useShallow((s) => ({
      font: s.font,
      fontScale: s.fontScale,
      radius: s.radius,
      contentWidth: s.contentWidth,
      motion: s.motion,
      tableHeader: s.tableHeader,
      tableBorder: s.tableBorder,
      tableSize: s.tableSize,
      tableStripe: s.tableStripe,
    })),
  );
}
