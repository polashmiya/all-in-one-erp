export type DiscountType = 'percent' | 'amount' | 'unit';

export interface SalesLine {
  quantity?: number | null;
  price?: number | null;
  discount?: number | null;
  discountType?: DiscountType;
  sdPercentage?: number | null;
  vatPercentage?: number | null;
  aitPercentage?: number | null;
  isOfferItem?: boolean;
}

export interface SalesHeader {
  isVatInclusive?: boolean | null;
  isGrossDiscount?: boolean | null;
  discountPercentage?: number | null;
  discountAmount?: number | null;
  charges: number;
  reductions?: number;
}

const round = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100;

const sum = (values: number[]) => values.reduce((total, value) => total + value, 0);

const lineTotal = (line: SalesLine | undefined) =>
  line?.isOfferItem ? 0 : round((line?.quantity ?? 0) * (line?.price ?? 0));

function lineDiscount(line: SalesLine | undefined, total: number) {
  const discount = line?.isOfferItem ? 0 : (line?.discount ?? 0);
  if (line?.discountType === 'amount') return discount;
  if (line?.discountType === 'unit') return round(discount * (line.quantity ?? 0));
  return round((total * discount) / 100);
}

function sharedDiscounts(totals: number[], header: SalesHeader) {
  const gross = sum(totals);
  if (!gross) return totals.map(() => 0);
  const percentage = header.discountPercentage ?? 0;
  const discount =
    percentage > 0 ? round((gross * percentage) / 100) : (header.discountAmount ?? 0);
  const shares = totals.map((total) => round((discount * total) / gross));
  const last = totals.findLastIndex((total) => total > 0);
  const rest = round(discount - sum(shares));
  return shares.map((share, index) => (index === last ? round(share + rest) : share));
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
  const totals = lines.map(lineTotal);
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
  const reductions = header.reductions ?? 0;
  return {
    quantity: sum(lines.map((line) => line?.quantity ?? 0)),
    gross: sum(prices.map((p) => p.total)),
    discount: sum(prices.map((p) => p.discount)),
    sd: sum(prices.map((p) => p.sd)),
    vat: sum(prices.map((p) => p.vat)),
    ait: sum(prices.map((p) => p.ait)),
    charges: header.charges,
    reductions,
    total: sum(prices.map((p) => p.value)) + header.charges - reductions,
  };
}

interface SavedDiscount {
  quantity: number;
  discountPercentage: number;
  discountAmount: number;
  isAmountWiseDiscount: boolean;
  isDiscountPerQuantity: boolean;
}

export function discountOf(line: SavedDiscount): { discount: number; discountType: DiscountType } {
  if (line.isDiscountPerQuantity)
    return { discount: line.discountAmount / line.quantity, discountType: 'unit' };
  if (line.isAmountWiseDiscount) return { discount: line.discountAmount, discountType: 'amount' };
  return { discount: line.discountPercentage, discountType: 'percent' };
}

export function discountFields(line: SalesLine) {
  const byPercent = line.discountType === 'percent' || !line.discountType;
  return {
    discountPercentage: byPercent ? (line.discount ?? null) : null,
    discountAmount: byPercent ? null : (line.discount ?? null),
    isAmountWiseDiscount: !byPercent,
    isDiscountPerQuantity: line.discountType === 'unit',
  };
}
