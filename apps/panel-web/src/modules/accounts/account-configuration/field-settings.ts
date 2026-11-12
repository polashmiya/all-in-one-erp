export const FIELD_WIDTHS = [1, 2, 3, 4];

export const LABEL_MAX = 100;

export const LOOKUP_TYPE = 'lookup';

export const TEXT_TYPES = ['text', 'textarea'];

export const NUMBER_TYPES = ['number', 'decimal', 'money', 'percent'];

export function effective<T extends object>(row: { base: T; override?: T | null }): T {
  const own = Object.entries(row.override ?? {}).filter(([, value]) => value != null);
  return { ...row.base, ...Object.fromEntries(own) };
}

const clean = (value: unknown) =>
  typeof value === 'string' ? value.trim() || null : (value ?? null);

export function toOverride<T extends object>(values: T, base: T): T {
  return Object.fromEntries(
    Object.entries(values).map(([key, value]) => {
      const own = clean(value);
      return [key, own === clean(base[key as keyof T]) ? null : own];
    }),
  ) as T;
}
