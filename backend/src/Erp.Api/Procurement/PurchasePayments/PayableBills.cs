using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchasePayments;

internal static class PayableBills
{
    public static Task<Dictionary<int, decimal>> PaidAsync(
        ErpDbContext db, IReadOnlyCollection<int> receiveIds, int exceptPaymentId, CancellationToken cancellationToken) =>
        db.PurchasePayments.AsNoTracking()
            .Where(p => p.IsActive && p.Status != ApprovalStatus.Rejected && p.Id != exceptPaymentId)
            .SelectMany(p => p.Lines)
            .Where(l => receiveIds.Contains(l.PurchaseReceiveId))
            .GroupBy(l => l.PurchaseReceiveId)
            .Select(g => new { g.Key, Paid = g.Sum(l => l.PaymentAmount + l.Vds + l.Tds + l.DiscountAmount) })
            .ToDictionaryAsync(g => g.Key, g => g.Paid, cancellationToken);

    public static async Task SyncAsync(ErpDbContext db, IEnumerable<int> receiveIds, CancellationToken cancellationToken)
    {
        var ids = receiveIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var paid = await PaidAsync(db, ids, 0, cancellationToken);
        var receives = await db.PurchaseReceives.Where(r => ids.Contains(r.Id)).ToListAsync(cancellationToken);
        foreach (var receive in receives) receive.PaidAmount = paid.GetValueOrDefault(receive.Id);
        await db.SaveChangesAsync(cancellationToken);
    }
}
