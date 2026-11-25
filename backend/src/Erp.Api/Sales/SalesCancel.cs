using Erp.Api.Approvals;
using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales;

internal static class SalesCancel
{
    private static readonly ApprovalStatus[] Gone = [ApprovalStatus.Rejected, ApprovalStatus.Cancelled];

    public static async Task<ProblemHttpResult?> QuotationAsync(ErpDbContext db, int quotationId, CancellationToken cancellationToken) =>
        Used("sales orders", await db.SalesOrders
            .Where(o => o.IsActive && o.SalesQuotationId == quotationId && !Gone.Contains(o.Status))
            .Select(o => o.Code).ToListAsync(cancellationToken));

    public static async Task<ProblemHttpResult?> OrderAsync(ErpDbContext db, int orderId, CancellationToken cancellationToken) =>
        Used("deliveries", await db.SalesDeliveries
            .Where(d => d.IsActive && d.OrderId == orderId && !Gone.Contains(d.Status))
            .Select(d => d.Code).ToListAsync(cancellationToken));

    public static async Task<ProblemHttpResult?> PendingDeliveriesAsync(ErpDbContext db, int orderId, CancellationToken cancellationToken) =>
        Used("pending deliveries", await db.SalesDeliveries
            .Where(d => d.IsActive && d.OrderId == orderId && d.Status == ApprovalStatus.Pending)
            .Select(d => d.Code).ToListAsync(cancellationToken));

    private static ProblemHttpResult? Used(string what, List<string> codes) =>
        codes.Count == 0 ? null : ApprovalFlow.InUse($"It has {what}: {string.Join(", ", codes)}.");
}
