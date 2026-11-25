using Erp.Api.Approvals;
using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;

namespace Erp.Api.Sales;

internal sealed class QuotationApprovalSource : IApprovalSource
{
    public string Feature => SalesMenu.SalesQuotation;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.SalesQuotations
            .Where(q => q.IsActive && (status == null || q.Status == status) && (ids == null || ids.Contains(q.Id)))
            .Select(q => new ApprovalDocumentInfo(
                q.Id,
                q.Code,
                q.QuotationDate,
                q.CustomerName,
                q.TotalAmount,
                q.Status,
                db.ErpUsers.Where(u => u.Id == q.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                q.CreatedAt));
}
