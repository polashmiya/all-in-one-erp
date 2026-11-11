import { theme } from 'antd';
import {
  defaultUiPrefs,
  fontKeys,
  fontStacks,
  sanitizeUiPrefs,
  scaledFontSizes,
  scaledRadii,
} from './appearance';
import { buildBrand, shade, siderStyles, type ThemeMode } from './brand';
import { contrast, luminance, mix } from './color';
import { CUSTOM_PRESET, DEFAULT_PRESET, findPreset, resolvePreset, themePresets } from './presets';
import { buildTheme, fontFamily } from './tokens';

const modes: ThemeMode[] = ['light', 'dark'];

describe('colour helpers', () => {
  it('mixes, shades and measures contrast', () => {
    expect(mix('#000000', '#FFFFFF', 0.5)).toBe('#808080');
    expect(mix('#0F766E', '#FFFFFF', 0)).toBe('#FFFFFF');
    expect(shade('#FFFFFF', 0.5)).toBe('#808080');
    expect(contrast('#000000', '#FFFFFF')).toBeCloseTo(21, 0);
    expect(luminance('#FFFFFF')).toBeCloseTo(1);
  });
});

describe('theme presets', () => {
  it('defaults to teal', () => {
    expect(DEFAULT_PRESET).toBe('teal');
    expect(findPreset('unknown').key).toBe('teal');
    expect(findPreset(undefined).primary).toBe('#0F766E');
  });

  it('has unique keys', () => {
    expect(new Set(themePresets.map((p) => p.key)).size).toBe(themePresets.length);
  });

  it.each(themePresets.map((p) => [p.key, p.primary]))(
    '%s primary carries white text at WCAG AA',
    (_, primary) => {
      expect(contrast('#FFFFFF', primary)).toBeGreaterThanOrEqual(4.5);
    },
  );

  it('resolves a custom primary and ignores invalid ones', () => {
    expect(resolvePreset(CUSTOM_PRESET, '#123abc').primary).toBe('#123ABC');
    expect(resolvePreset(CUSTOM_PRESET, 'red').key).toBe('teal');
    expect(resolvePreset('indigo', '#123abc').key).toBe('indigo');
  });
});

describe('buildBrand', () => {
  const combos = themePresets.flatMap((preset) =>
    modes.flatMap((mode) => siderStyles.map((sider) => [preset.key, mode, sider] as const)),
  );

  it.each(combos)('%s / %s / %s sidebar keeps menu text readable', (key, mode, sider) => {
    const colors = buildBrand(findPreset(key), mode, sider).sider;

    expect(contrast(colors.text, colors.bg)).toBeGreaterThanOrEqual(4.5);
    expect(contrast(colors.activeText, colors.activeBg)).toBeGreaterThanOrEqual(4.5);
    expect(contrast(colors.muted, colors.bg)).toBeGreaterThanOrEqual(3);
  });

  it('uses the design kit surfaces for the light sidebar', () => {
    const brand = buildBrand(findPreset('teal'), 'light', 'light');

    expect(brand.sider.bg).toBe('#F2F3F5');
    expect(brand.frameBg).toBe(brand.sider.bg);
    expect(brand.layoutBg).toBe('#F7F8FA');
    expect(brand.sider.activeIcon).toBe('#0F766E');
    expect(brand.sider.inverse).toBe(false);
  });

  it('derives the brand sidebar from the primary', () => {
    const brand = buildBrand(findPreset('teal'), 'light', 'brand');

    expect(brand.sider.bg).toBe(shade('#0F766E', 0.66));
    expect(brand.sider.inverse).toBe(true);
    expect(brand.sider.panelBorder).toBe('transparent');
  });

  it('uses dark surfaces in dark mode', () => {
    const brand = buildBrand(findPreset('teal'), 'dark', 'light');

    expect(luminance(brand.layoutBg)).toBeLessThan(0.02);
    expect(luminance(brand.containerBg)).toBeLessThan(0.03);
    expect(brand.sider.inverse).toBe(true);
  });

  it.each(themePresets.map((p) => [p.key]))('%s primary stays readable in dark mode', (key) => {
    const brand = buildBrand(findPreset(key), 'dark', 'light');

    expect(contrast(brand.primary, brand.containerBg)).toBeGreaterThanOrEqual(3);
    expect(contrast(brand.primaryInk, brand.primary)).toBeGreaterThanOrEqual(4.5);
  });
});

describe('buildTheme', () => {
  const brand = buildBrand(findPreset('teal'), 'light', 'light');

  it('maps the brand onto AntD tokens', () => {
    const config = buildTheme({ mode: 'light', compact: false, brand });

    expect(config.algorithm).toEqual([theme.defaultAlgorithm]);
    expect(config.token?.colorPrimary).toBe('#0F766E');
    expect(config.token?.colorBgLayout).toBe('#F7F8FA');
    expect(config.token?.borderRadius).toBe(8);
    expect(config.token?.controlHeight).toBe(38);
    expect(config.token?.fontFamily).toBe(fontFamily);
  });

  it('switches to the dark algorithm in dark mode', () => {
    const dark = buildBrand(findPreset('teal'), 'dark', 'light');
    expect(buildTheme({ mode: 'dark', compact: false, brand: dark }).algorithm).toEqual([
      theme.darkAlgorithm,
    ]);
  });

  it('adds the compact algorithm when compact density is on', () => {
    expect(buildTheme({ mode: 'light', compact: true, brand }).algorithm).toEqual([
      theme.defaultAlgorithm,
      theme.compactAlgorithm,
    ]);
  });

  it('keeps a Bangla-capable font in the stack', () => {
    expect(fontFamily).toContain('Hind Siliguri');
    for (const key of fontKeys) expect(fontStacks[key]).toContain('Hind Siliguri');
  });

  it('applies font, size, radius, motion and header prefs', () => {
    const config = buildTheme({
      mode: 'light',
      compact: false,
      brand,
      prefs: {
        font: 'roboto',
        fontScale: 'lg',
        radius: 'round',
        motion: false,
        tableHeader: 'solid',
      },
    });

    expect(config.token?.fontFamily).toBe(fontStacks.roboto);
    expect(config.token?.fontSize).toBe(15);
    expect(config.token?.borderRadius).toBe(12);
    expect(config.token?.motion).toBe(false);
    expect(config.components?.Table?.headerBg).toBe(brand.primary);
    expect(config.components?.Table?.headerColor).toBe('#FFFFFF');
  });
});

describe('appearance prefs', () => {
  it('falls back to defaults for unknown or missing values', () => {
    expect(sanitizeUiPrefs({})).toEqual(defaultUiPrefs);
    expect(sanitizeUiPrefs({ font: 'comic', tableStripe: 'yes', tableSize: 'compact' })).toEqual({
      ...defaultUiPrefs,
      tableSize: 'compact',
    });
  });

  it('scales font sizes and radii', () => {
    expect(scaledFontSizes('md').base).toBe(14);
    expect(scaledFontSizes('xl').base).toBe(16);
    expect(scaledRadii('sharp').md).toBe(2);
    expect(scaledRadii('round').pill).toBe(999);
  });
});
