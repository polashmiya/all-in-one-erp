import dayjs, { type ConfigType } from 'dayjs';

const LOCALE = 'en-IN';

export const CURRENCY_SYMBOL = '৳';

const integerFormat = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 0 });
const moneyFormat = new Intl.NumberFormat(LOCALE, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function isNumber(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isFinite(value);
}

export function formatNumber(value: number | null | undefined, fractionDigits = 0): string {
  if (!isNumber(value)) return '—';
  if (fractionDigits === 0) return integerFormat.format(value);
  return new Intl.NumberFormat(LOCALE, {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(value);
}

export function formatMoney(
  value: number | null | undefined,
  { symbol = true }: { symbol?: boolean } = {},
): string {
  if (!isNumber(value)) return '—';
  const amount = moneyFormat.format(value);
  return symbol ? `${CURRENCY_SYMBOL} ${amount}` : amount;
}

export function formatMoneyCompact(value: number | null | undefined): string {
  if (!isNumber(value)) return '—';
  const abs = Math.abs(value);
  const trim = (n: number) => n.toFixed(2).replace(/\.?0+$/, '');
  if (abs >= 1e7) return `${CURRENCY_SYMBOL} ${trim(value / 1e7)} Cr`;
  if (abs >= 1e5) return `${CURRENCY_SYMBOL} ${trim(value / 1e5)} L`;
  return `${CURRENCY_SYMBOL} ${integerFormat.format(value)}`;
}

export function formatPercent(value: number | null | undefined, fractionDigits = 2): string {
  if (!isNumber(value)) return '—';
  return `${value.toFixed(fractionDigits)} %`;
}

export function formatDelta(value: number, suffix = '%'): string {
  const sign = value > 0 ? '+' : value < 0 ? '−' : '';
  return `${sign}${Math.abs(value)}${suffix}`;
}

export const DATE_FORMAT = 'D MMM YYYY';
export const API_DATE_FORMAT = 'YYYY-MM-DD';
export const DATE_TIME_FORMAT = 'D MMM YYYY, HH:mm';

export function formatDate(value: ConfigType, format = DATE_FORMAT): string {
  if (value === null || value === undefined || value === '') return '—';
  const date = dayjs(value);
  return date.isValid() ? date.format(format) : '—';
}

export function formatDateTime(value: ConfigType): string {
  return formatDate(value, DATE_TIME_FORMAT);
}

export function initials(name: string | null | undefined, max = 2): string {
  const all = (name ?? '').trim().split(/\s+/).filter(Boolean);
  const lettered = all.filter((w) => /^\p{L}/u.test(w));
  const words = lettered.length ? lettered : all;
  if (words.length === 0) return '?';
  if (words.length === 1) return words[0]!.slice(0, max).toUpperCase();
  return words
    .slice(0, max)
    .map((w) => w[0])
    .join('')
    .toUpperCase();
}

export function pageRange(
  page: number,
  pageSize: number,
  total: number,
): { from: number; to: number } {
  if (total === 0) return { from: 0, to: 0 };
  const from = (page - 1) * pageSize + 1;
  return { from, to: Math.min(total, page * pageSize) };
}

export type TimeOfDay = 'morning' | 'afternoon' | 'evening';

export function timeOfDay(date = new Date()): TimeOfDay {
  const hour = date.getHours();
  if (hour < 12) return 'morning';
  if (hour < 17) return 'afternoon';
  return 'evening';
}
