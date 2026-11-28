import { fontSize, radius } from './design-tokens';

export const fontKeys = [
  'onest',
  'inter',
  'roboto',
  'openSans',
  'manrope',
  'poppins',
  'system',
] as const;
export const fontScales = ['sm', 'md', 'lg', 'xl'] as const;
export const radiusStyles = ['sharp', 'soft', 'round'] as const;
export const contentWidths = ['fluid', 'boxed'] as const;
export const tableHeaderStyles = ['tinted', 'neutral', 'solid'] as const;
export const tableBorderStyles = ['grid', 'rows', 'none'] as const;
export const tableSizes = ['compact', 'default', 'comfortable'] as const;

export type FontKey = (typeof fontKeys)[number];
export type FontScale = (typeof fontScales)[number];
export type RadiusStyle = (typeof radiusStyles)[number];
export type ContentWidth = (typeof contentWidths)[number];
export type TableHeaderStyle = (typeof tableHeaderStyles)[number];
export type TableBorderStyle = (typeof tableBorderStyles)[number];
export type TableSize = (typeof tableSizes)[number];

export interface UiPrefs {
  font: FontKey;
  fontScale: FontScale;
  radius: RadiusStyle;
  contentWidth: ContentWidth;
  motion: boolean;
  tableHeader: TableHeaderStyle;
  tableBorder: TableBorderStyle;
  tableSize: TableSize;
  tableStripe: boolean;
}

export const defaultUiPrefs: UiPrefs = {
  font: 'onest',
  fontScale: 'md',
  radius: 'soft',
  contentWidth: 'fluid',
  motion: true,
  tableHeader: 'solid',
  tableBorder: 'grid',
  tableSize: 'compact',
  tableStripe: true,
};

const fallbackStack =
  "'Hind Siliguri', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

export const fontStacks: Record<FontKey, string> = {
  onest: `'Onest Variable', 'Onest', ${fallbackStack}`,
  inter: `'Inter Variable', 'Inter', ${fallbackStack}`,
  roboto: `'Roboto Variable', 'Roboto', ${fallbackStack}`,
  openSans: `'Open Sans Variable', 'Open Sans', ${fallbackStack}`,
  manrope: `'Manrope Variable', 'Manrope', ${fallbackStack}`,
  poppins: `'Poppins', ${fallbackStack}`,
  system: `system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Hind Siliguri', Roboto, 'Helvetica Neue', Arial, sans-serif`,
};

export const fontNames: Record<FontKey, string> = {
  onest: 'Onest',
  inter: 'Inter',
  roboto: 'Roboto',
  openSans: 'Open Sans',
  manrope: 'Manrope',
  poppins: 'Poppins',
  system: 'System',
};

const fontBase: Record<FontScale, number> = { sm: 13, md: 14, lg: 15, xl: 16 };

const radiusFactor: Record<RadiusStyle, number> = { sharp: 0.25, soft: 1, round: 1.5 };

export const tableMetrics: Record<
  TableSize,
  {
    row: number;
    rowSm: number;
    head: number;
    headSm: number;
    avatar: number;
    action: number;
    check: number;
  }
> = {
  compact: { row: 30, rowSm: 26, head: 32, headSm: 28, avatar: 22, action: 24, check: 16 },
  default: { row: 40, rowSm: 32, head: 36, headSm: 32, avatar: 28, action: 28, check: 18 },
  comfortable: { row: 50, rowSm: 40, head: 42, headSm: 36, avatar: 32, action: 30, check: 20 },
};

export type FontSizes = { [K in keyof typeof fontSize]: number };
export type Radii = { [K in keyof typeof radius]: number };

export function scaledFontSizes(scale: FontScale): FontSizes {
  const factor = fontBase[scale] / fontBase.md;
  return Object.fromEntries(
    Object.entries(fontSize).map(([key, value]) => [key, Math.round(value * factor)]),
  ) as FontSizes;
}

export function scaledRadii(style: RadiusStyle): Radii {
  const factor = radiusFactor[style];
  return Object.fromEntries(
    Object.entries(radius).map(([key, value]) => [
      key,
      key === 'pill' ? value : Math.round(value * factor),
    ]),
  ) as Radii;
}

function oneOf<T>(value: unknown, allowed: readonly T[], fallback: T): T {
  return allowed.includes(value as T) ? (value as T) : fallback;
}

export function sanitizeUiPrefs(value: Partial<Record<keyof UiPrefs, unknown>>): UiPrefs {
  const d = defaultUiPrefs;
  return {
    font: oneOf(value.font, fontKeys, d.font),
    fontScale: oneOf(value.fontScale, fontScales, d.fontScale),
    radius: oneOf(value.radius, radiusStyles, d.radius),
    contentWidth: oneOf(value.contentWidth, contentWidths, d.contentWidth),
    motion: typeof value.motion === 'boolean' ? value.motion : d.motion,
    tableHeader: oneOf(value.tableHeader, tableHeaderStyles, d.tableHeader),
    tableBorder: oneOf(value.tableBorder, tableBorderStyles, d.tableBorder),
    tableSize: oneOf(value.tableSize, tableSizes, d.tableSize),
    tableStripe: typeof value.tableStripe === 'boolean' ? value.tableStripe : d.tableStripe,
  };
}
