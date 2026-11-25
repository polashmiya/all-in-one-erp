export type DiscountType = 'percent' | 'amount' | 'unit';

export interface SalesLine {
  quantity?: number | null;
  price?: number | null;
  discount?: number | null;
  discountType?: DiscountType;
  sdPercentage?: number | null;
  vatPercentage?: number | null;
  aitPercentage?: number | null;
}

export interface SalesHeader {
  isVatInclusive?: boolean | null;
  isGrossDiscount?: boolean | null;
  discountPercentage?: number | null;
  discountAmount?: number | null;
  deliveryCharge?: number | null;
  extraCharge?: number | null;
}

const round = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100;

const sum = (values: number[]) => values.reduce((total, value) => total + value, 0);

function lineDiscount(line: SalesLine | undefined, total: number) {
  const discount = line?.discount ?? 0;
  if (line?.discountType === 'amount') return discount;
  if (line?.discountType === 'unit') return round(discount * (line.quantity ?? 0));
  return round((total * discount) / 100);
}

function sharedDiscounts(totals: number[], header: SalesHeader) {
  const gross = sum(totals);
  const percentage = header.discountPercentage ?? 0;
  const discount =
    percentage > 0 ? round((gross * percentage) / 100) : (header.discountAmount ?? 0);
  let left = discount;
  return totals.map((total, index) => {
    const last = index === totals.length - 1;
    const share = last ? left : gross ? round((discount * total) / gross) : 0;
    left = round(left - share);
    return share;
  });
}

function taxes(line: SalesLine | undefined, amount: number, vatInclusive: boolean) {
  const [sdRate, vatRate, aitRate] = [
    line?.sdPercentage ?? 0,
    line?.vatPercentage ?? 0,
    line?.aitPercentage ?? 0,
  ];
  const net = vatInclusive ? amount / (1 + vatRate / 100) / (1 + sdRate / 100) : amount;
  const sd = round((net * sdRate) / 100);
  const vat = round(((net + sd) * vatRate) / 100);
  const ait = round((net * aitRate) / 100);
  return { sd, vat, ait, value: vatInclusive ? amount : amount + sd + vat + ait };
}

export function salesPrices(lines: (SalesLine | undefined)[], header: SalesHeader) {
  const totals = lines.map((line) => round((line?.quantity ?? 0) * (line?.price ?? 0)));
  const discounts = header.isGrossDiscount
    ? sharedDiscounts(totals, header)
    : lines.map((line, index) => lineDiscount(line, totals[index] ?? 0));

  return lines.map((line, index) => {
    const total = totals[index] ?? 0;
    const discount = discounts[index] ?? 0;
    return { total, discount, ...taxes(line, total - discount, !!header.isVatInclusive) };
  });
}

export function salesTotals(lines: (SalesLine | undefined)[], header: SalesHeader) {
  const prices = salesPrices(lines, header);
  const charges = (header.deliveryCharge ?? 0) + (header.extraCharge ?? 0);
  return {
    quantity: sum(lines.map((line) => line?.quantity ?? 0)),
    gross: sum(prices.map((p) => p.total)),
    discount: sum(prices.map((p) => p.discount)),
    sd: sum(prices.map((p) => p.sd)),
    vat: sum(prices.map((p) => p.vat)),
    ait: sum(prices.map((p) => p.ait)),
    charges,
    total: sum(prices.map((p) => p.value)) + charges,
  };
}
