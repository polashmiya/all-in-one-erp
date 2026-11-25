using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesCollections;

internal static class CustomerAdvances
{
    public static async Task<decimal> AvailableAsync(
        ErpDbContext db, int customerId, int exceptCollectionId, CancellationToken cancellationToken)
    {
        var received = await db.SalesCollections.AsNoTracking()
            .Where(c => c.IsActive && c.CustomerId == customerId && c.IsAdvance && c.Status == ApprovalStatus.Approved)
            .SumAsync(c => c.CollectionAmount, cancellationToken);
        var kept = await db.SalesReturns.AsNoTracking()
            .Where(r => r.IsActive && r.CustomerId == customerId && r.Status == ApprovalStatus.Approved)
            .SumAsync(r => r.AdvanceAmount, cancellationToken);
        var left = await db.SalesOrders.AsNoTracking()
            .Where(o => o.IsActive && o.CustomerId == customerId && o.Status == ApprovalStatus.Closed)
            .SumAsync(o => o.AdvanceRemainingAmount, cancellationToken);
        var used = await DeliveryDues.LiveCollections(db)
            .Where(c => c.CustomerId == customerId && c.Id != exceptCollectionId)
            .SumAsync(c => c.AdvanceAdjusted, cancellationToken);
        return received + kept + left - used;
    }
}
