using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesDeliveries;

internal static class OrderDeliveries
{
    private static IQueryable<SalesDelivery> Live(ErpDbContext db, IReadOnlyCollection<int> orderIds) =>
        db.SalesDeliveries.AsNoTracking()
            .Where(d => d.IsActive && d.Status != ApprovalStatus.Rejected && d.Status != ApprovalStatus.Cancelled &&
                d.OrderId != null && orderIds.Contains(d.OrderId.Value));

    public static async Task<Dictionary<int, (decimal Quantity, decimal Discount)>> UsedAsync(
        ErpDbContext db, IReadOnlyCollection<int> orderIds, int exceptDeliveryId, CancellationToken cancellationToken)
    {
        var lines = await Live(db, orderIds).Where(d => d.Id != exceptDeliveryId)
            .SelectMany(d => d.Lines.Select(l => new { l.OrderLineId, l.Quantity, l.DiscountAmount }))
            .ToListAsync(cancellationToken);
        return lines.Where(l => l.OrderLineId is not null).GroupBy(l => l.OrderLineId!.Value)
            .ToDictionary(g => g.Key, g => (g.Sum(l => l.Quantity), g.Sum(l => l.DiscountAmount)));
    }

    public static decimal Discount(SalesLine line, decimal quantity, (decimal Quantity, decimal Discount) used) =>
        quantity >= line.Quantity - used.Quantity
            ? Math.Max(0, line.DiscountAmount - used.Discount)
            : SalesPricing.Round(line.DiscountAmount * quantity / line.Quantity);

    public static async Task<decimal> AdvanceAvailableAsync(
        ErpDbContext db, SalesOrder order, int exceptDeliveryId, CancellationToken cancellationToken) =>
        order.AdvanceAmount - await Live(db, [order.Id]).Where(d => d.Id != exceptDeliveryId)
            .SumAsync(d => d.AdvanceBalanceAdjust, cancellationToken);

    public static async Task SyncAsync(ErpDbContext db, IEnumerable<int?> orderIds, CancellationToken cancellationToken)
    {
        var ids = orderIds.OfType<int>().Distinct().ToList();
        if (ids.Count == 0) return;

        var deliveries = await Live(db, ids).Include(d => d.Lines).ToListAsync(cancellationToken);
        var orders = await db.SalesOrders.Include(o => o.Lines).Where(o => ids.Contains(o.Id)).ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            var own = deliveries.Where(d => d.OrderId == order.Id).ToList();
            var pending = own.Where(d => d.Status == ApprovalStatus.Pending).SelectMany(d => d.Lines).ToLookup(l => l.OrderLineId);
            var done = own.Where(d => d.Status != ApprovalStatus.Pending).SelectMany(d => d.Lines).ToLookup(l => l.OrderLineId);
            foreach (var line in order.Lines)
            {
                line.ScheduledQuantity = pending[line.Id].Sum(l => l.Quantity);
                line.DeliveredQuantity = done[line.Id].Sum(l => l.Quantity);
            }

            order.DeliveredQuantity = order.Lines.Sum(l => l.DeliveredQuantity);
            order.IsCompleted = order.Lines.All(l => l.DeliveredQuantity >= l.Quantity);
            order.AdjustedAmount = own.Sum(d => d.AdvanceBalanceAdjust);
            order.ReceiveAmount = order.AdvanceAmount + own.Where(d => d.Status != ApprovalStatus.Pending)
                .Sum(d => d.ReceiveAmount + d.CollectedAmount);
            order.PendingAmount = order.NetAmount - order.ReceiveAmount - own.Sum(d => d.ReturnAdjusted);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
