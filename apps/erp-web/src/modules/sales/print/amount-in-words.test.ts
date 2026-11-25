import { describe, expect, it } from 'vitest';
import { amountInWords, type Translate } from './amount-in-words';
import { salesPrintResources } from './i18n';

const translate =
  (language: 'en' | 'bn'): Translate =>
  (key, values = {}) =>
    Object.entries(values).reduce(
      (text, [name, value]) => text.replace(`{{${name}}}`, value),
      salesPrintResources[language][key] ?? key,
    );

describe('amountInWords', () => {
  it.each(['en', 'bn'] as const)('has one word for every number below 100 in %s', (language) => {
    const numbers = salesPrintResources[language]['print.words.numbers']?.split(',') ?? [];
    expect(new Set(numbers).size).toBe(100);
  });

  it.each([
    [0, 'Taka Zero Only'],
    [115, 'Taka One Hundred Fifteen Only'],
    [100000, 'Taka One Lakh Only'],
    [
      12345678.5,
      'Taka One Crore Twenty Three Lakh Forty Five Thousand Six Hundred Seventy Eight and Fifty Paisa Only',
    ],
    [1.005, 'Taka One Only'],
    [99.999, 'Taka One Hundred Only'],
  ])('writes %s in English', (amount, text) => {
    expect(amountInWords(amount, translate('en'))).toBe(text);
  });

  it('writes Bangla with lakh and paisa', () => {
    expect(amountInWords(125075.25, translate('bn'))).toBe(
      'এক লক্ষ পঁচিশ হাজার পঁচাত্তর টাকা পঁচিশ পয়সা মাত্র',
    );
  });
});
