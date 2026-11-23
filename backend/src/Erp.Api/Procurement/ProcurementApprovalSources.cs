using Erp.Api.Approvals;
using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;

namespace Erp.Api.Procurement;

internal sealed class RequisitionApprovalSource : IApprovalSource
{
    public string Feature => ProcurementMenu.PurchaseRequisition;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.PurchaseRequisitions
            .Where(r => r.IsActive && (status == null || r.Status == status) && (ids == null || ids.Contains(r.Id)))
            .Select(r => new ApprovalDocumentInfo(
                r.Id,
                r.Code,
                r.RequestDate,
                db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                r.TotalAmount,
                r.Status,
                db.ErpUsers.Where(u => u.Id == r.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                r.CreatedAt));
}

internal sealed class OrderApprovalSource : IApprovalSource
{
    public string Feature => ProcurementMenu.PurchaseOrder;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.PurchaseOrders
            .Where(o => o.IsActive && (status == null || o.Status == status) && (ids == null || ids.Contains(o.Id)))
            .Select(o => new ApprovalDocumentInfo(
                o.Id,
                o.Code,
                o.OrderDate,
                db.Suppliers.Where(s => s.Id == o.SupplierId).Select(s => s.Name).FirstOrDefault(),
                o.NetAmount,
                o.Status,
                db.ErpUsers.Where(u => u.Id == o.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                o.CreatedAt));
}

internal sealed class ReceiveApprovalSource : IApprovalSource
{
    public string Feature => ProcurementMenu.PurchaseReceive;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.PurchaseReceives
            .Where(r => r.IsActive && (status == null || r.Status == status) && (ids == null || ids.Contains(r.Id)))
            .Select(r => new ApprovalDocumentInfo(
                r.Id,
                r.Code,
                r.ReceiveDate,
                db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefault(),
                r.NetAmount,
                r.Status,
                db.ErpUsers.Where(u => u.Id == r.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                r.CreatedAt));
}

internal sealed class ReturnApprovalSource : IApprovalSource
{
    public string Feature => ProcurementMenu.PurchaseReturn;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.PurchaseReturns
            .Where(r => r.IsActive && (status == null || r.Status == status) && (ids == null || ids.Contains(r.Id)))
            .Select(r => new ApprovalDocumentInfo(
                r.Id,
                r.Code,
                r.ReturnDate,
                db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefault(),
                r.NetAmount,
                r.Status,
                db.ErpUsers.Where(u => u.Id == r.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                r.CreatedAt));
}
