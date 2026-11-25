using Erp.Core.Domain.Sales;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesReturns;

internal sealed record ReturnedLine(decimal Quantity, decimal Discount, decimal Cost)
{
    public static readonly ReturnedLine None = new(0, 0, 0);
}

internal static class DeliveryReturns
{
    public static async Task<Dictionary<int, ReturnedLine>> LinesAsync(
        ErpDbContext db, IReadOnlyCollection<int> deliveryIds, int exceptReturnId, CancellationToken cancellationToken)
    {
        var lines = await DeliveryDues.LiveReturns(db)
            .Where(r => r.Id != exceptReturnId && deliveryIds.Contains(r.DeliveryId ?? 0))
            .SelectMany(r => r.Lines)
            .ToListAsync(cancellationToken);
        return lines.GroupBy(l => l.DeliveryLineId).ToDictionary(
            g => g.Key ?? 0,
            g => new ReturnedLine(g.Sum(l => l.Quantity), g.Sum(l => l.DiscountAmount), g.Sum(l => l.CostTotal)));
    }

    public static decimal Cost(SalesDeliveryLine line, decimal quantity, ReturnedLine returned) =>
        quantity >= line.Quantity - returned.Quantity
            ? Math.Max(0, line.CostTotal - returned.Cost)
            : SalesPricing.Round(line.CostTotal * quantity / line.Quantity);

    public static decimal Reductions(SalesDelivery delivery) => delivery.SpecialDiscountAmount + delivery.DeductionValue;

    public static decimal ReductionRate(SalesDelivery delivery)
    {
        var value = delivery.Lines.Sum(l => l.TotalValue);
        return value == 0 ? 0 : Math.Round(Reductions(delivery) / value, 8);
    }

    public static async Task<decimal> ChargeRefundableAsync(
        ErpDbContext db, SalesDelivery delivery, int exceptReturnId, CancellationToken cancellationToken) =>
        delivery.OthersCost + delivery.ExtraCharge - await DeliveryDues.LiveReturns(db)
            .Where(r => r.Id != exceptReturnId && r.DeliveryId == delivery.Id)
            .SumAsync(r => r.ChargeRefundAmount, cancellationToken);
}
