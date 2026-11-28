import type { LocalizedText } from '../i18n/language';

export interface ThemePreset {
  key: string;
  name: LocalizedText;
  primary: string;
  accent: string;
}

export const themePresets = [
  { key: 'teal', name: { en: 'Teal', bn: 'টিল' }, primary: '#0F766E', accent: '#F59E0B' },
  { key: 'indigo', name: { en: 'Indigo', bn: 'ইন্ডিগো' }, primary: '#4F46E5', accent: '#06B6D4' },
  { key: 'blue', name: { en: 'Blue', bn: 'নীল' }, primary: '#1D4ED8', accent: '#F97316' },
  { key: 'violet', name: { en: 'Violet', bn: 'বেগুনি' }, primary: '#7C3AED', accent: '#EC4899' },
  { key: 'emerald', name: { en: 'Emerald', bn: 'পান্না' }, primary: '#047857', accent: '#8B5CF6' },
  { key: 'rose', name: { en: 'Rose', bn: 'গোলাপ' }, primary: '#BE123C', accent: '#0EA5E9' },
  { key: 'orange', name: { en: 'Orange', bn: 'কমলা' }, primary: '#C2410C', accent: '#14B8A6' },
  { key: 'slate', name: { en: 'Slate', bn: 'স্লেট' }, primary: '#334155', accent: '#14B8A6' },
] as const satisfies readonly ThemePreset[];

export type ThemePresetKey = (typeof themePresets)[number]['key'];

export const DEFAULT_PRESET: ThemePresetKey = 'emerald';

export const CUSTOM_PRESET = 'custom';

const HEX = /^#[0-9a-f]{6}$/i;

export function findPreset(key: string | undefined): ThemePreset {
  return (
    themePresets.find((p) => p.key === key) ?? themePresets.find((p) => p.key === DEFAULT_PRESET)!
  );
}

export function resolvePreset(key: string | undefined, customPrimary?: string | null): ThemePreset {
  if (key === CUSTOM_PRESET && customPrimary && HEX.test(customPrimary)) {
    return {
      key: CUSTOM_PRESET,
      name: { en: 'Custom', bn: 'কাস্টম' },
      primary: customPrimary.toUpperCase(),
      accent: themePresets[0].accent,
    };
  }
  return findPreset(key);
}
