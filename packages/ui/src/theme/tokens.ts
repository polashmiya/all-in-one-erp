import { theme, type ThemeConfig } from 'antd';
import {
  defaultUiPrefs,
  fontStacks,
  scaledFontSizes,
  scaledRadii,
  type UiPrefs,
} from './appearance';
import { tableHeadColors, type Brand, type ThemeMode } from './brand';
import { controlHeight, fontFamily, shadows, surfaces, tones } from './design-tokens';

export type { ThemeMode } from './brand';

export interface ThemeOptions {
  mode: ThemeMode;
  compact: boolean;
  brand: Brand;
  prefs?: Partial<UiPrefs>;
}

export { fontFamily };

export function buildTheme({ mode, compact, brand, prefs }: ThemeOptions): ThemeConfig {
  const ui = { ...defaultUiPrefs, ...prefs };
  const fontSize = scaledFontSizes(ui.fontScale);
  const radius = scaledRadii(ui.radius);
  const head = tableHeadColors(brand, ui.tableHeader);
  const light = mode === 'light';
  const algorithm = [light ? theme.defaultAlgorithm : theme.darkAlgorithm];
  if (compact) algorithm.push(theme.compactAlgorithm);

  return {
    algorithm,
    token: {
      colorPrimary: brand.primary,
      colorInfo: brand.info,
      colorSuccess: tones[mode].success.dot,
      colorWarning: tones[mode].warning.dot,
      colorError: surfaces[mode].required,
      colorLink: brand.primary,
      colorLinkHover: brand.primaryHover,
      colorBgLayout: brand.layoutBg,
      colorBgContainer: brand.containerBg,
      colorBorder: brand.border,
      colorBorderSecondary: brand.borderSecondary,
      colorSplit: brand.divider,
      colorText: brand.text,
      colorTextHeading: brand.textStrong,
      colorTextSecondary: brand.textSecondary,
      colorTextTertiary: brand.textMuted,
      colorTextDescription: brand.textMuted,
      colorTextPlaceholder: surfaces[mode].placeholder,
      colorFillAlter: brand.tableHeaderBg,
      controlOutline: brand.primarySoft,
      controlOutlineWidth: 3,
      borderRadius: radius.md,
      borderRadiusLG: radius.lg,
      borderRadiusSM: radius.sm,
      controlHeight: controlHeight.input,
      controlHeightSM: controlHeight.sm,
      controlHeightLG: controlHeight.lg,
      fontFamily: fontStacks[ui.font],
      fontSize: fontSize.base,
      motion: ui.motion,
      boxShadowSecondary: `${shadows[mode].popover}, 0 0 0 1px ${brand.borderSecondary}`,
      wireframe: false,
    },
    components: {
      Button: {
        controlHeight: controlHeight.md,
        controlHeightSM: controlHeight.sm,
        controlHeightLG: controlHeight.lg,
        paddingInline: 14,
        primaryShadow: 'none',
        defaultShadow: 'none',
        dangerShadow: 'none',
        fontWeight: 500,
        defaultBorderColor: brand.border,
        primaryColor: brand.primaryInk,
      },
      Card: { headerFontSize: fontSize.lg, headerHeight: 52, paddingLG: 20 },
      Menu: {
        itemBorderRadius: radius.md,
        subMenuItemBorderRadius: radius.md,
        itemMarginInline: 6,
        itemHeight: 36,
      },
      Dropdown: { paddingBlock: 8 },
      Table: {
        headerBg: head.bg,
        headerSortActiveBg: head.sortBg,
        headerSortHoverBg: head.sortBg,
        fixedHeaderSortActiveBg: head.sortBg,
        headerColor: head.fg,
        headerSplitColor: 'transparent',
        headerBorderRadius: 0,
        borderColor: brand.divider,
        rowHoverBg: brand.rowHoverBg,
        rowSelectedBg: brand.tintBg,
        rowSelectedHoverBg: brand.tintBg,
        cellPaddingBlock: 6,
        cellPaddingBlockSM: 4,
        cellPaddingInline: 12,
        cellFontSize: fontSize.md,
      },
      Tabs: {
        horizontalMargin: '0',
        horizontalItemPadding: '13px 12px',
        horizontalItemGutter: 4,
        itemColor: brand.textSecondary,
        titleFontSize: fontSize.base,
      },
      Segmented: {
        trackBg: brand.trackBg,
        trackPadding: 3,
        itemColor: brand.textSecondary,
        itemSelectedColor: brand.textStrong,
        itemSelectedBg: brand.containerBg,
        borderRadius: radius.md,
        borderRadiusSM: radius.sm,
      },
      Tag: {
        defaultBg: tones[mode].neutral.bg,
        defaultColor: brand.textSecondary,
      },
      Switch: { trackHeight: 24, trackMinWidth: 40, handleSize: 20 },
      Checkbox: { borderRadiusSM: radius.xs },
      Steps: { iconSize: 30 },
      Modal: { borderRadiusLG: radius.xl },
      Popover: { borderRadiusLG: radius.xl },
      Drawer: { colorBgElevated: brand.containerBg },
      Pagination: { itemSize: controlHeight.sm, itemActiveBg: brand.textStrong },
    },
  };
}
