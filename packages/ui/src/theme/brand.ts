import { generate } from '@ant-design/colors';
import type { TableHeaderStyle } from './appearance';
import { alpha, contrast, mix } from './color';
import { shadows, sidebarPalettes, surfaces, tones } from './design-tokens';
import type { ThemePreset } from './presets';

export type ThemeMode = 'light' | 'dark';

export type SiderStyle = 'light' | 'dark' | 'brand';

export const siderStyles: SiderStyle[] = ['light', 'dark', 'brand'];

export interface SiderColors {
  bg: string;
  divider: string;
  text: string;
  strong: string;
  muted: string;
  icon: string;
  hoverBg: string;
  guide: string;
  inputBg: string;
  inputShadow: string;
  kbd: string;
  activeBg: string;
  activeText: string;
  activeIcon: string;
  activeShadow: string;
  bar: string;
  badgeBg: string;
  badgeText: string;
  panelBorder: string;
  inverse: boolean;
}

export interface Brand {
  key: string;
  mode: ThemeMode;
  primary: string;
  primaryHover: string;
  primarySoft: string;
  primaryInk: string;
  accent: string;
  palette: string[];
  accentPalette: string[];
  accentIcon: string;
  info: string;
  frameBg: string;
  layoutBg: string;
  containerBg: string;
  headerBg: string;
  headerBorder: string;
  border: string;
  borderSecondary: string;
  divider: string;
  tableHeaderBg: string;
  tableHeadTint: string;
  tableHeadTintStrong: string;
  stripeBg: string;
  rowHoverBg: string;
  tintBg: string;
  textStrong: string;
  text: string;
  textSecondary: string;
  textMuted: string;
  trackBg: string;
  sider: SiderColors;
  deep: string;
  cardShadow: string;
}

const WHITE = '#FFFFFF';
const BLACK = '#000000';
const MIN_SURFACE_CONTRAST = 3;
const MIN_TEXT_CONTRAST = 4.5;
const DARK_PRIMARY_STEPS = [5, 4, 3, 2, 1];

export function shade(color: string, amount: number): string {
  return mix(BLACK, color, amount);
}

function inkOn(color: string): string {
  const dark = shade(color, 0.85);
  return contrast(WHITE, color) >= contrast(dark, color) ? WHITE : dark;
}

function readablePrimary(palette: string[], surface: string): string {
  const steps = DARK_PRIMARY_STEPS.map((step) => palette[step]!);
  return (
    steps.find(
      (color) =>
        contrast(color, surface) >= MIN_SURFACE_CONTRAST &&
        contrast(inkOn(color), color) >= MIN_TEXT_CONTRAST,
    ) ?? steps.at(-1)!
  );
}

function siderColors(
  style: SiderStyle,
  mode: ThemeMode,
  primary: string,
  palette: string[],
): SiderColors {
  if (style === 'brand') {
    const bg = shade(primary, 0.66);
    const activeBg = mix(WHITE, bg, 0.16);
    return {
      bg,
      divider: alpha(WHITE, 0.1),
      text: mix(WHITE, bg, 0.84),
      strong: WHITE,
      muted: mix(WHITE, bg, 0.66),
      icon: mix(WHITE, bg, 0.72),
      hoverBg: alpha(WHITE, 0.08),
      guide: alpha(WHITE, 0.18),
      inputBg: alpha(WHITE, 0.08),
      inputShadow: `inset 0 0 0 1px ${alpha(WHITE, 0.08)}`,
      kbd: alpha(WHITE, 0.22),
      activeBg,
      activeText: WHITE,
      activeIcon: WHITE,
      activeShadow: `inset 0 0 0 1px ${alpha(WHITE, 0.1)}`,
      bar: WHITE,
      badgeBg: alpha(WHITE, 0.18),
      badgeText: WHITE,
      panelBorder: 'transparent',
      inverse: true,
    };
  }

  if (style === 'dark' || mode === 'dark') {
    const dark = sidebarPalettes.dark;
    const bg = style === 'dark' ? dark.bg : sidebarPalettes.darkMode.bg;
    return {
      bg,
      divider: alpha(WHITE, 0.07),
      text: dark.text,
      strong: WHITE,
      muted: dark.muted,
      icon: dark.muted,
      hoverBg: alpha(WHITE, 0.06),
      guide: dark.guide,
      inputBg: alpha(WHITE, 0.06),
      inputShadow: `inset 0 0 0 1px ${alpha(WHITE, 0.06)}`,
      kbd: alpha(WHITE, 0.14),
      activeBg: mix(WHITE, bg, 0.1),
      activeText: WHITE,
      activeIcon: mode === 'dark' ? palette[4]! : WHITE,
      activeShadow: `inset 0 0 0 1px ${alpha(WHITE, 0.06)}`,
      bar: mode === 'dark' ? palette[4]! : primary,
      badgeBg: alpha(WHITE, 0.12),
      badgeText: surfaces.dark.textStrong!,
      panelBorder: 'transparent',
      inverse: true,
    };
  }

  const light = sidebarPalettes.light;
  return {
    bg: light.bg,
    divider: light.divider,
    text: light.text,
    strong: light.strong,
    muted: light.muted,
    icon: light.icon,
    hoverBg: surfaces.light.rowHoverOverlay!,
    guide: light.guide,
    inputBg: light.activeBg,
    inputShadow: `0 0 0 1px ${light.divider}, ${shadows.light.card}`,
    kbd: light.kbd,
    activeBg: light.activeBg,
    activeText: light.strong,
    activeIcon: primary,
    activeShadow: `0 0 0 1px ${light.divider}, ${shadows.light.card}`,
    bar: primary,
    badgeBg: light.badgeBg,
    badgeText: light.text,
    panelBorder: light.divider,
    inverse: false,
  };
}

export function tableHeadColors(brand: Brand, style: TableHeaderStyle) {
  if (style === 'solid') {
    const bg = brand.mode === 'light' ? brand.primary : shade(brand.primary, 0.2);
    return { bg, fg: WHITE, sortBg: shade(bg, 0.14), line: alpha(WHITE, 0.18) };
  }
  if (style === 'neutral')
    return {
      bg: brand.tableHeaderBg,
      fg: brand.textSecondary,
      sortBg: brand.trackBg,
      line: brand.divider,
    };
  return {
    bg: brand.tableHeadTint,
    fg: brand.textSecondary,
    sortBg: brand.tableHeadTintStrong,
    line: brand.divider,
  };
}

export function buildBrand(preset: ThemePreset, mode: ThemeMode, siderStyle: SiderStyle): Brand {
  const { primary, accent } = preset;
  const palette = generate(primary);
  const accentPalette = generate(accent);
  const light = mode === 'light';
  const sider = siderColors(siderStyle, mode, primary, palette);

  const tone = surfaces[mode];
  const containerBg = tone.surface!;
  const main = light ? primary : readablePrimary(palette, containerBg);

  return {
    key: preset.key,
    mode,
    primary: main,
    primaryHover: light ? shade(primary, 0.18) : mix(main, WHITE, 0.85),
    primarySoft: light ? mix(primary, WHITE, 0.12) : mix(main, containerBg, 0.24),
    primaryInk: inkOn(main),
    accent,
    palette,
    accentPalette,
    accentIcon: light ? accentPalette[6]! : accent,
    info: tones[mode].info.fg,
    frameBg: sider.bg,
    layoutBg: tone.panel!,
    containerBg,
    headerBg: containerBg,
    headerBorder: tone.headerBorder!,
    border: tone.border!,
    borderSecondary: tone.borderSecondary!,
    divider: tone.divider!,
    tableHeaderBg: tone.tableHead!,
    tableHeadTint: light ? mix(primary, WHITE, 0.08) : mix(primary, containerBg, 0.16),
    tableHeadTintStrong: light ? mix(primary, WHITE, 0.14) : mix(primary, containerBg, 0.24),
    stripeBg: light ? mix(primary, WHITE, 0.03) : mix(primary, containerBg, 0.06),
    rowHoverBg: tone.rowHover!,
    tintBg: light ? mix(primary, WHITE, 0.12) : mix(primary, containerBg, 0.24),
    textStrong: tone.textStrong!,
    text: tone.text!,
    textSecondary: tone.textSecondary!,
    textMuted: tone.textMuted!,
    trackBg: tone.track!,
    sider,
    deep: shade(primary, 0.6),
    cardShadow: shadows[mode].card,
  };
}
