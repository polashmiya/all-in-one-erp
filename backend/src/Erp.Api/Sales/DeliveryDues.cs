using Erp.Api.Sales.SalesDeliveries;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales;

internal static class DeliveryDues
{
    public static IQueryable<SalesReturn> LiveReturns(ErpDbContext db) =>
        db.SalesReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != ApprovalStatus.Rejected && r.Status != ApprovalStatus.Cancelled);

    public static IQueryable<SalesCollection> LiveCollections(ErpDbContext db) =>
        db.SalesCollections.AsNoTracking()
            .Where(c => c.IsActive && c.Status != ApprovalStatus.Rejected && c.Status != ApprovalStatus.Cancelled);

    public static async Task<Dictionary<int, decimal>> DueAsync(
        ErpDbContext db, IReadOnlyCollection<int> deliveryIds, int exceptReturnId, int exceptCollectionId, CancellationToken cancellationToken)
    {
        var billed = await db.SalesDeliveries.AsNoTracking()
            .Where(d => deliveryIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.CustomerNetAmount - d.ReceiveAmount, cancellationToken);
        var returned = await ReturnAdjustedAsync(db, deliveryIds, exceptReturnId, cancellationToken);
        var collected = await CollectedAsync(db, deliveryIds, exceptCollectionId, cancellationToken);
        return billed.ToDictionary(
            b => b.Key, b => b.Value - returned.GetValueOrDefault(b.Key) - collected.GetValueOrDefault(b.Key));
    }

    public static async Task SyncAsync(ErpDbContext db, IEnumerable<int> deliveryIds, CancellationToken cancellationToken)
    {
        var ids = deliveryIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var collected = await CollectedAsync(db, ids, 0, cancellationToken);
        var returns = await LiveReturns(db).Include(r => r.Lines).Where(r => ids.Contains(r.DeliveryId ?? 0)).ToListAsync(cancellationToken);
        var deliveries = await db.SalesDeliveries.Include(d => d.Lines).Where(d => ids.Contains(d.Id)).ToListAsync(cancellationToken);
        foreach (var delivery in deliveries)
        {
            var own = returns.Where(r => r.DeliveryId == delivery.Id).ToList();
            var lines = own.SelectMany(r => r.Lines).ToLookup(l => l.DeliveryLineId);
            foreach (var line in delivery.Lines) line.ReturnedQuantity = lines[line.Id].Sum(l => l.Quantity);
            delivery.ReturnAmount = own.Sum(r => r.NetAmount);
            delivery.ReturnAdjusted = own.Sum(r => r.DueAdjusted);
            delivery.CollectedAmount = collected.GetValueOrDefault(delivery.Id);
            delivery.PendingAmount =
                delivery.CustomerNetAmount - delivery.ReceiveAmount - delivery.ReturnAdjusted - delivery.CollectedAmount;
        }

        await db.SaveChangesAsync(cancellationToken);
        await OrderDeliveries.SyncAsync(db, deliveries.Select(d => d.OrderId), cancellationToken);
    }

    private static Task<Dictionary<int, decimal>> ReturnAdjustedAsync(
        ErpDbContext db, IReadOnlyCollection<int> deliveryIds, int exceptReturnId, CancellationToken cancellationToken) =>
        LiveReturns(db)
            .Where(r => r.Id != exceptReturnId && deliveryIds.Contains(r.DeliveryId ?? 0))
            .GroupBy(r => r.DeliveryId!.Value)
            .Select(g => new { g.Key, Amount = g.Sum(r => r.DueAdjusted) })
            .ToDictionaryAsync(g => g.Key, g => g.Amount, cancellationToken);

    private static Task<Dictionary<int, decimal>> CollectedAsync(
        ErpDbContext db, IReadOnlyCollection<int> deliveryIds, int exceptCollectionId, CancellationToken cancellationToken) =>
        LiveCollections(db)
            .Where(c => c.Id != exceptCollectionId)
            .SelectMany(c => c.Lines)
            .Where(l => deliveryIds.Contains(l.DeliveryId))
            .GroupBy(l => l.DeliveryId)
            .Select(g => new { g.Key, Amount = g.Sum(l => l.SettledAmount) })
            .ToDictionaryAsync(g => g.Key, g => g.Amount, cancellationToken);
}
