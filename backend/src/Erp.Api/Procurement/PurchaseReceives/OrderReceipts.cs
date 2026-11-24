using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchaseReceives;

internal static class OrderReceipts
{
    public static async Task<decimal> ToleranceAsync(ErpDbContext db, int orderId, CancellationToken cancellationToken) =>
        await db.PurchaseOrders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => db.Suppliers.Where(s => s.Id == o.SupplierId).Select(s => s.Tolerance).FirstOrDefault())
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

    public static decimal Allowed(decimal ordered, decimal tolerance) => Math.Round(ordered * (1 + tolerance / 100), 4);

    public static async Task<Dictionary<(int, int), decimal>> ReceivedAsync(
        ErpDbContext db, IReadOnlyCollection<int> orderIds, int exceptReceiveId, CancellationToken cancellationToken)
    {
        var lines = await db.PurchaseReceives.AsNoTracking()
            .Where(r => r.IsActive && r.Status != ApprovalStatus.Rejected && r.Status != ApprovalStatus.Cancelled && r.Id != exceptReceiveId && orderIds.Contains(r.PurchaseOrderId))
            .SelectMany(r => r.Lines.Select(l => new { r.PurchaseOrderId, l.ItemId, l.Quantity }))
            .ToListAsync(cancellationToken);
        return lines.GroupBy(l => (l.PurchaseOrderId, l.ItemId)).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
    }

    public static async Task SyncAsync(ErpDbContext db, IEnumerable<int?> orderIds, CancellationToken cancellationToken)
    {
        var ids = orderIds.OfType<int>().Distinct().ToList();
        if (ids.Count == 0) return;

        var received = await ReceivedAsync(db, ids, 0, cancellationToken);
        var orders = await db.PurchaseOrders.Include(o => o.Lines).Where(o => ids.Contains(o.Id)).ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            foreach (var line in order.Lines) line.ReceivedQuantity = received.GetValueOrDefault((order.Id, line.ItemId));
            order.ReceivedQuantity = order.Lines.Sum(l => l.ReceivedQuantity);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
