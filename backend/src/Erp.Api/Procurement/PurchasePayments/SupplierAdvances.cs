using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchasePayments;

internal static class SupplierAdvances
{
    public static async Task<decimal> AvailableAsync(
        ErpDbContext db, int supplierId, int exceptPaymentId, CancellationToken cancellationToken)
    {
        var payments = db.PurchasePayments.AsNoTracking().Where(p => p.IsActive && p.SupplierId == supplierId && p.Id != exceptPaymentId);
        var paid = await payments
            .Where(p => p.IsAdvance && p.Status == ApprovalStatus.Approved)
            .SumAsync(p => p.PaymentAmount, cancellationToken);
        var used = await payments
            .Where(p => !p.IsAdvance && p.Status != ApprovalStatus.Rejected && p.Status != ApprovalStatus.Cancelled)
            .SumAsync(p => p.AdvanceAdjusted, cancellationToken);
        return paid - used;
    }
}
