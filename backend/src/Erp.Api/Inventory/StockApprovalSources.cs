using Erp.Api.Approvals;
using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;

namespace Erp.Api.Inventory;

internal sealed class StockAdjustmentApprovalSource : IApprovalSource
{
    public string Feature => InventoryMenu.StockAdjustment;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.StockAdjustments
            .Where(a => a.IsActive && (status == null || a.Status == status) && (ids == null || ids.Contains(a.Id)))
            .Select(a => new ApprovalDocumentInfo(
                a.Id,
                a.Code,
                a.AdjustmentDate,
                db.Warehouses.Where(w => w.Id == a.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                a.TotalAmount,
                a.Status,
                db.ErpUsers.Where(u => u.Id == a.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                a.CreatedAt));
}

internal sealed class StockTransferApprovalSource : IApprovalSource
{
    public string Feature => InventoryMenu.StockTransfer;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.StockTransfers
            .Where(t => t.IsActive && (status == null || t.Status == status) && (ids == null || ids.Contains(t.Id)))
            .Select(t => new ApprovalDocumentInfo(
                t.Id,
                t.Code,
                t.TransferDate,
                db.Warehouses.Where(w => w.Id == t.ToWarehouseId).Select(w => w.Name).FirstOrDefault(),
                0,
                t.Status,
                db.ErpUsers.Where(u => u.Id == t.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                t.CreatedAt));
}
