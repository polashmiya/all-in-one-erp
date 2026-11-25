export type Translate = (key: string, values?: Record<string, string>) => string;

const UNITS = [
  { size: 10_000_000, key: 'crore' },
  { size: 100_000, key: 'lakh' },
  { size: 1_000, key: 'thousand' },
  { size: 100, key: 'hundred' },
] as const;

function words(value: number, t: Translate): string {
  const unit = UNITS.find(({ size }) => value >= size);
  if (!unit) return t('print.words.numbers').split(',')[value] ?? '';
  const head = `${words(Math.floor(value / unit.size), t)} ${t(`print.words.${unit.key}`)}`;
  const rest = value % unit.size;
  return rest ? `${head} ${words(rest, t)}` : head;
}

export function amountInWords(amount: number, t: Translate): string {
  const total = Math.round(amount * 100);
  const taka = words(Math.floor(total / 100), t);
  const paisa = total % 100;
  return paisa
    ? t('print.words.withPaisa', { taka, paisa: words(paisa, t) })
    : t('print.words.only', { taka });
}
