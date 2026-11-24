import { describe, expect, it } from 'vitest';
import { journalTotals } from './journal-form';

describe('journalTotals', () => {
  it('adds each side and gives the difference without rounding noise', () => {
    const totals = journalTotals([{ debit: 0.1 }, { debit: 0.2 }, { credit: 0.3 }]);

    expect(totals).toEqual({ debit: 0.3, credit: 0.3, difference: 0 });
  });

  it('shows how much is missing', () => {
    expect(journalTotals([{ debit: 100 }, { credit: 90 }, {}]).difference).toBe(10);
  });
});
