using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchaseReturns;

internal static class ReceiveReturns
{
    public static async Task<Dictionary<(int, int), decimal>> ReturnedAsync(
        ErpDbContext db, IReadOnlyCollection<int> receiveIds, int exceptReturnId, CancellationToken cancellationToken)
    {
        var lines = await db.PurchaseReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != ApprovalStatus.Rejected && r.Id != exceptReturnId &&
                receiveIds.Contains(r.PurchaseReceiveId))
            .SelectMany(r => r.Lines.Select(l => new { r.PurchaseReceiveId, l.ItemId, l.Quantity }))
            .ToListAsync(cancellationToken);
        return lines.GroupBy(l => (l.PurchaseReceiveId, l.ItemId)).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
    }

    public static async Task SyncAsync(ErpDbContext db, IEnumerable<int?> receiveIds, CancellationToken cancellationToken)
    {
        var ids = receiveIds.OfType<int>().Distinct().ToList();
        if (ids.Count == 0) return;

        var returned = await ReturnedAsync(db, ids, 0, cancellationToken);
        var amounts = await db.PurchaseReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != ApprovalStatus.Rejected && ids.Contains(r.PurchaseReceiveId))
            .GroupBy(r => r.PurchaseReceiveId)
            .Select(g => new { g.Key, Amount = g.Sum(r => r.NetAmount) })
            .ToDictionaryAsync(g => g.Key, g => g.Amount, cancellationToken);
        var receives = await db.PurchaseReceives.Include(r => r.Lines).Where(r => ids.Contains(r.Id)).ToListAsync(cancellationToken);
        foreach (var receive in receives)
        {
            foreach (var line in receive.Lines) line.ReturnedQuantity = returned.GetValueOrDefault((receive.Id, line.ItemId));
            receive.ReturnAmount = amounts.GetValueOrDefault(receive.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
