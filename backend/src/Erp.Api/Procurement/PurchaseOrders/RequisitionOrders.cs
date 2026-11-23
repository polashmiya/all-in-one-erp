using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchaseOrders;

internal static class RequisitionOrders
{
    public static async Task<Dictionary<int, decimal>> RemainingAsync(
        ErpDbContext db, int requisitionId, int exceptOrderId, CancellationToken cancellationToken)
    {
        var ordered = await OrderedAsync(db, [requisitionId], exceptOrderId, cancellationToken);
        var rows = await db.PurchaseRequisitionRows.AsNoTracking()
            .Where(r => r.RequisitionId == requisitionId)
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.ItemId, r => r.Quantity - ordered.GetValueOrDefault((requisitionId, r.ItemId)));
    }

    public static async Task SyncAsync(ErpDbContext db, IEnumerable<int?> requisitionIds, CancellationToken cancellationToken)
    {
        var ids = requisitionIds.OfType<int>().Distinct().ToList();
        if (ids.Count == 0) return;

        var ordered = await OrderedAsync(db, ids, 0, cancellationToken);
        var rows = await db.PurchaseRequisitionRows.Where(r => ids.Contains(r.RequisitionId)).ToListAsync(cancellationToken);
        foreach (var row in rows) row.OrderedQuantity = ordered.GetValueOrDefault((row.RequisitionId, row.ItemId));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<(int, int), decimal>> OrderedAsync(
        ErpDbContext db, List<int> requisitionIds, int exceptOrderId, CancellationToken cancellationToken)
    {
        var lines = await db.PurchaseOrders.AsNoTracking()
            .Where(o => o.IsActive && o.Status != ApprovalStatus.Rejected && o.Id != exceptOrderId &&
                o.RequisitionId != null && requisitionIds.Contains(o.RequisitionId.Value))
            .SelectMany(o => o.Lines.Select(l => new { RequisitionId = o.RequisitionId!.Value, l.ItemId, l.Quantity }))
            .ToListAsync(cancellationToken);
        return lines.GroupBy(l => (l.RequisitionId, l.ItemId)).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
    }
}
