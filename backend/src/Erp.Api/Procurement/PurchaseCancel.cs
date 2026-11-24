using Erp.Api.Approvals;
using Erp.Api.Inventory;
using Erp.Api.Procurement.PurchasePayments;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Procurement;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement;

internal static class PurchaseCancel
{
    private static readonly ApprovalStatus[] Gone = [ApprovalStatus.Rejected, ApprovalStatus.Cancelled];

    public static async Task<ProblemHttpResult?> RequisitionAsync(
        ErpDbContext db, ICurrentContext current, PurchaseRequisition requisition, CancellationToken cancellationToken) =>
        Used("purchase orders", await db.PurchaseOrders
            .Where(o => o.IsActive && o.RequisitionId == requisition.Id && !Gone.Contains(o.Status))
            .Select(o => o.Code).ToListAsync(cancellationToken));

    public static async Task<ProblemHttpResult?> OrderAsync(
        ErpDbContext db, ICurrentContext current, PurchaseOrder order, CancellationToken cancellationToken) =>
        Used("goods receives", await db.PurchaseReceives
            .Where(r => r.IsActive && r.PurchaseOrderId == order.Id && !Gone.Contains(r.Status))
            .Select(r => r.Code).ToListAsync(cancellationToken));

    public static async Task<ProblemHttpResult?> ReceiveAsync(
        ErpDbContext db, ICurrentContext current, PurchaseReceive receive, CancellationToken cancellationToken)
    {
        var returns = await db.PurchaseReturns
            .Where(r => r.IsActive && r.PurchaseReceiveId == receive.Id && !Gone.Contains(r.Status))
            .Select(r => r.Code).ToListAsync(cancellationToken);
        var payments = await db.PurchasePayments
            .Where(p => p.IsActive && !Gone.Contains(p.Status) && p.Lines.Any(l => l.PurchaseReceiveId == receive.Id))
            .Select(p => p.Code).ToListAsync(cancellationToken);
        return Used("returns", returns) ?? Used("payments", payments) ??
            await StockPosting.CancelAsync(db, current, ProcurementMenu.PurchaseReceive, receive.Id, receive.ReceiveDate, cancellationToken);
    }

    public static Task<ProblemHttpResult?> ReturnAsync(
        ErpDbContext db, ICurrentContext current, PurchaseReturn purchaseReturn, CancellationToken cancellationToken) =>
        StockPosting.CancelAsync(
            db, current, ProcurementMenu.PurchaseReturn, purchaseReturn.Id, purchaseReturn.ReturnDate, cancellationToken);

    public static async Task<ProblemHttpResult?> PaymentAsync(
        ErpDbContext db, ICurrentContext current, PurchasePayment payment, CancellationToken cancellationToken)
    {
        var deposits = await db.TaxDeposits
            .Where(d => d.IsActive && !Gone.Contains(d.Status) && d.Lines.Any(l => l.PurchasePaymentId == payment.Id))
            .Select(d => d.Code).ToListAsync(cancellationToken);
        if (Used("VDS/TDS deposits", deposits) is { } used) return used;
        if (payment.IsAdvance &&
            await SupplierAdvances.AvailableAsync(db, payment.SupplierId, 0, cancellationToken) < payment.PaymentAmount)
        {
            return ApprovalFlow.InUse("Other payments already use this advance. Cancel them first.");
        }

        return await StockPosting.CancelAsync(
            db, current, ProcurementMenu.PurchasePayment, payment.Id, payment.PaymentDate, cancellationToken);
    }

    private static ProblemHttpResult? Used(string what, List<string> codes) =>
        codes.Count == 0 ? null : ApprovalFlow.InUse($"It has {what}: {string.Join(", ", codes)}. Cancel them first.");
}
