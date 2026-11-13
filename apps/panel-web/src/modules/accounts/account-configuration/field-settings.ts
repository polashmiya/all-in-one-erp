export const FIELD_WIDTHS = [1, 2, 3, 4];

export const LABEL_MAX = 100;

export const LOOKUP_TYPE = 'lookup';

export const TEXT_TYPES = ['text', 'textarea'];

export const NUMBER_TYPES = ['number', 'decimal', 'money', 'percent'];

export function effective<T extends object>(row: { base: T; override?: T | null }): T {
  const own = Object.entries(row.override ?? {}).filter(([, value]) => value != null);
  return { ...row.base, ...Object.fromEntries(own) };
}

/** Where a field sits on the screen; owned by the layout page. */
export const PLACEMENT_KEYS = ['sectionKey', 'sortOrder'];

/** Whether the scope's own values differ from what it would otherwise get, leaving out `ignore`. */
export function isChanged<T extends object>(
  row: { base: T; override?: T | null },
  ignore: string[] = [],
): boolean {
  return Object.entries(row.override ?? {}).some(
    ([key, value]) => value != null && !ignore.includes(key) && value !== row.base[key as keyof T],
  );
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
