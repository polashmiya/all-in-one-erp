using Erp.Core.Domain.Sales;

namespace Erp.Api.Sales;

internal sealed record DiscountRule(bool VatInclusive, bool OnTotal, decimal Percentage, decimal Amount, string AmountField);

internal static class SalesPricing
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static void Price(IReadOnlyList<SalesLine> lines, DiscountRule rule, Dictionary<string, string[]> errors)
    {
        foreach (var line in lines) line.TotalAmount = Round(line.Quantity * line.Price);

        if (rule.OnTotal)
        {
            var total = lines.Sum(l => l.TotalAmount);
            var discount = rule.Percentage > 0 ? Round(total * rule.Percentage / 100) : rule.Amount;
            if (discount > total) errors[rule.AmountField] = ["The discount cannot be more than the item total."];
            else Share(lines, discount, total);
        }
        else
        {
            foreach (var line in lines) LineDiscount(line);
            if (lines.Any(l => l.DiscountAmount > l.TotalAmount)) errors["lines"] = ["A discount cannot be more than the line amount."];
        }

        foreach (var line in lines) Taxes(line, rule.VatInclusive);
    }

    public static decimal[] Shares(decimal amount, IReadOnlyList<decimal> weights)
    {
        var total = weights.Sum();
        var shares = weights.Select(w => total == 0 ? 0 : Round(amount * w / total)).ToArray();
        var last = weights.ToList().FindLastIndex(w => w > 0);
        if (last >= 0) shares[last] += amount - shares.Sum();
        return shares;
    }

    private static void LineDiscount(SalesLine line)
    {
        line.DiscountAmount = line.IsDiscountPerQuantity
            ? Round(line.DiscountAmount * line.Quantity)
            : line.IsAmountWiseDiscount ? line.DiscountAmount : Round(line.TotalAmount * line.DiscountPercentage / 100);
        if (line.IsAmountWiseDiscount) line.DiscountPercentage = 0;
    }

    private static void Share(IReadOnlyList<SalesLine> lines, decimal discount, decimal total)
    {
        var shares = Shares(total == 0 ? 0 : discount, [.. lines.Select(l => l.TotalAmount)]);
        for (var i = 0; i < lines.Count; i++)
        {
            lines[i].DiscountAmount = shares[i];
            lines[i].DiscountPercentage = 0;
            lines[i].IsAmountWiseDiscount = true;
            lines[i].IsDiscountPerQuantity = false;
        }
    }

    private static void Taxes(SalesLine line, bool vatInclusive)
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
