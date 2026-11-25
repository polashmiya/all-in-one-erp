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

internal sealed class SalesOrderApprovalSource : IApprovalSource
{
    public string Feature => SalesMenu.SalesOrder;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.SalesOrders
            .Where(d => d.IsActive && (status == null || d.Status == status) && (ids == null || ids.Contains(d.Id)))
            .Select(d => new ApprovalDocumentInfo(
                d.Id,
                d.Code,
                d.OrderDate,
                d.CustomerName,
                d.NetAmount,
                d.Status,
                db.ErpUsers.Where(u => u.Id == d.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                d.CreatedAt));
}

internal sealed class SalesDeliveryApprovalSource : IApprovalSource
{
    public string Feature => SalesMenu.SalesDelivery;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.SalesDeliveries
            .Where(d => d.IsActive && (status == null || d.Status == status) && (ids == null || ids.Contains(d.Id)))
            .Select(d => new ApprovalDocumentInfo(
                d.Id,
                d.Code,
                d.DeliveryDate,
                d.CustomerName,
                d.NetAmount,
                d.Status,
                db.ErpUsers.Where(u => u.Id == d.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                d.CreatedAt));
}
