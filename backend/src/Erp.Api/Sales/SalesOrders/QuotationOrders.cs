using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesOrders;

internal static class QuotationOrders
{
    public static IQueryable<SalesQuotation> Open(ErpDbContext db) =>
        db.SalesQuotations.AsNoTracking()
            .Where(q => q.IsActive && q.Status == ApprovalStatus.Approved && !q.IsOrdered && !q.IsExpired);

    public static async Task SyncAsync(ErpDbContext db, IEnumerable<int?> quotationIds, CancellationToken cancellationToken)
    {
        var ids = quotationIds.OfType<int>().Distinct().ToList();
        if (ids.Count == 0) return;

        var orders = await db.SalesOrders.AsNoTracking()
            .Where(o => o.IsActive && o.SalesQuotationId != null && ids.Contains(o.SalesQuotationId.Value))
            .Select(o => new { QuotationId = o.SalesQuotationId!.Value, o.Status })
            .ToListAsync(cancellationToken);
        foreach (var quotation in await db.SalesQuotations.Where(q => ids.Contains(q.Id)).ToListAsync(cancellationToken))
        {
            var own = orders.Where(o => o.QuotationId == quotation.Id).Select(o => o.Status).ToList();
            quotation.IsOrdered = own.Any(s => s is not (ApprovalStatus.Rejected or ApprovalStatus.Cancelled));
            quotation.OrderStatus = quotation.IsOrdered
                ? QuotationOrderStatus.Created
                : own.Contains(ApprovalStatus.Rejected) ? QuotationOrderStatus.Rejected : QuotationOrderStatus.NotCreated;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
