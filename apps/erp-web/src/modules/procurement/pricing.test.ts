import { pricedTotals, priced } from './pricing';

describe('order pricing', () => {
  it('adds VAT on top of the discounted amount', () => {
    expect(priced({ quantity: 2, price: 100, discount: 20, vatPercentage: 10 }, false)).toEqual({
      total: 200,
      vat: 18,
      net: 198,
    });
  });

  it('takes VAT out of an inclusive price', () => {
    expect(priced({ quantity: 1, price: 115, vatPercentage: 15 }, true)).toEqual({
      total: 115,
      vat: 15,
      net: 115,
    });
  });

  it('applies the header discount and charges to the item total', () => {
    const lines = [
      { quantity: 2, price: 100, discount: 20, vatPercentage: 10 },
      { quantity: 1, price: 50 },
    ];
    expect(pricedTotals(lines, { isVatInclusive: false, headerDiscount: 8, charges: 10 })).toEqual({
      quantity: 3,
      vat: 18,
      itemTotal: 248,
      net: 250,
    });
  });
});
