import { describe, expect, it } from 'vitest';
import { salesPrices, salesTotals } from './pricing';

describe('salesPrices', () => {
  it('adds SD, then VAT on the amount with SD, and AIT on the amount', () => {
    const [line] = salesPrices(
      [
        {
          quantity: 2,
          price: 100,
          discount: 10,
          discountType: 'percent',
          sdPercentage: 10,
          vatPercentage: 15,
          aitPercentage: 5,
        },
      ],
      { charges: 0 },
    );
    expect(line).toMatchObject({ total: 200, discount: 20, sd: 18, vat: 29.7, ait: 9 });
    expect(line?.value).toBeCloseTo(236.7);
  });

  it('takes a discount as an amount or per unit', () => {
    const prices = salesPrices(
      [
        { quantity: 1, price: 50, discount: 5, discountType: 'amount' },
        { quantity: 4, price: 25, discount: 2, discountType: 'unit' },
      ],
      { charges: 0 },
    );
    expect(prices.map((p) => p.value)).toEqual([45, 92]);
  });

  it('shares a discount on the total and keeps inclusive amounts', () => {
    const prices = salesPrices(
      [
        { quantity: 1, price: 230, vatPercentage: 15 },
        { quantity: 1, price: 100, discount: 50, discountType: 'percent' },
      ],
      { isVatInclusive: true, isGrossDiscount: true, discountPercentage: 10, charges: 0 },
    );
    expect(prices.map((p) => [p.discount, p.vat, p.value])).toEqual([
      [23, 27, 207],
      [10, 0, 90],
    ]);
  });
});

describe('salesTotals', () => {
  it('adds the charges to the line values', () => {
    const totals = salesTotals([{ quantity: 2, price: 50 }], {
      charges: 15,
      reductions: 5,
    });
    expect(totals).toMatchObject({ quantity: 2, gross: 100, charges: 15, total: 110 });
  });

  it('gives free items no price and no share of a discount', () => {
    const totals = salesTotals(
      [
        { quantity: 2, price: 50 },
        { quantity: 1, price: 50, isOfferItem: true },
      ],
      { isGrossDiscount: true, discountAmount: 10, charges: 0 },
    );
    expect(totals).toMatchObject({ quantity: 3, gross: 100, discount: 10, total: 90 });
  });
});
