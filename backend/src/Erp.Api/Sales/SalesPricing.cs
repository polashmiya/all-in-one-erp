using Erp.Core.Domain.Sales;

namespace Erp.Api.Sales;

internal static class SalesPricing
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static void Totals(IEnumerable<SalesQuotationLine> lines)
    {
        foreach (var line in lines) line.TotalAmount = Round(line.Quantity * line.Price);
    }

    public static decimal GrossDiscount(decimal total, decimal percentage, decimal amount) =>
        percentage > 0 ? Round(total * percentage / 100) : amount;

    public static void LineDiscount(SalesQuotationLine line)
    {
        line.DiscountAmount = line.IsDiscountPerQuantity
            ? Round(line.DiscountAmount * line.Quantity)
            : line.IsAmountWiseDiscount ? line.DiscountAmount : Round(line.TotalAmount * line.DiscountPercentage / 100);
        if (line.IsAmountWiseDiscount) line.DiscountPercentage = 0;
    }

    public static void ShareDiscount(IReadOnlyList<SalesQuotationLine> lines, decimal discount)
    {
        var total = lines.Sum(l => l.TotalAmount);
        var left = discount;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var share = i == lines.Count - 1 ? left : total == 0 ? 0 : Round(discount * line.TotalAmount / total);
            left -= share;
            line.DiscountAmount = share;
            line.DiscountPercentage = 0;
            line.IsAmountWiseDiscount = true;
            line.IsDiscountPerQuantity = false;
        }
    }

    public static void Taxes(SalesQuotationLine line, bool vatInclusive)
    {
        var amount = line.TotalAmount - line.DiscountAmount;
        var net = vatInclusive
            ? amount / (1 + line.VatPercentage / 100) / (1 + line.SdPercentage / 100)
            : amount;
        line.SdAmount = Round(net * line.SdPercentage / 100);
        line.VatAmount = Round((net + line.SdAmount) * line.VatPercentage / 100);
        line.AitAmount = Round(net * line.AitPercentage / 100);
        line.TotalValue = vatInclusive ? amount : amount + line.SdAmount + line.VatAmount + line.AitAmount;
    }
}
