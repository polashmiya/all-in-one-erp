export interface PricedLine {
  quantity?: number | null;
  price?: number | null;
  discount?: number | null;
  vatPercentage?: number | null;
}

export interface PricedHeader {
  isVatInclusive?: boolean | null;
  headerDiscount?: number | null;
  charges: number;
}

const round = (value: number) => Math.round(value * 100) / 100;

export function priced(line: PricedLine | undefined, vatInclusive: boolean) {
  const total = round((line?.quantity ?? 0) * (line?.price ?? 0));
  const taxable = total - (line?.discount ?? 0);
  const rate = line?.vatPercentage ?? 0;
  const vat = round(vatInclusive ? (taxable * rate) / (100 + rate) : (taxable * rate) / 100);
  return { total, vat, net: vatInclusive ? taxable : taxable + vat };
}

export function pricedTotals(lines: PricedLine[], header: PricedHeader) {
  const prices = lines.map((line) => priced(line, !!header.isVatInclusive));
  const itemTotal = prices.reduce((sum, p) => sum + p.net, 0);
  return {
    quantity: lines.reduce((sum, line) => sum + (line?.quantity ?? 0), 0),
    vat: prices.reduce((sum, p) => sum + p.vat, 0),
    itemTotal,
    net: itemTotal - (header.headerDiscount ?? 0) + header.charges,
  };
}
