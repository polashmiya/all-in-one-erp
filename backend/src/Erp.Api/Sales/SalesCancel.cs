using Erp.Api.Approvals;
using Erp.Api.Inventory;
using Erp.Api.Sales.SalesCollections;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Core.Tenancy;
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

    public static async Task<ProblemHttpResult?> DeliveryAsync(
        ErpDbContext db, ICurrentContext current, SalesDelivery delivery, CancellationToken cancellationToken)
    {
        if (delivery.IsConfirmed) return ApprovalFlow.InUse("The customer has already received it. Use a sales return.");
        var returns = await DeliveryDues.LiveReturns(db).Where(r => r.DeliveryId == delivery.Id).Select(r => r.Code)
            .ToListAsync(cancellationToken);
        var collections = await DeliveryDues.LiveCollections(db).Where(c => c.Lines.Any(l => l.DeliveryId == delivery.Id))
            .Select(c => c.Code).ToListAsync(cancellationToken);
        return Used("returns", returns) ?? Used("collections", collections) ??
            await StockPosting.CancelAsync(db, current, SalesMenu.SalesDelivery, delivery.Id, delivery.DeliveryDate, cancellationToken);
    }

    public static async Task<ProblemHttpResult?> ReturnAsync(
        ErpDbContext db, ICurrentContext current, SalesReturn salesReturn, CancellationToken cancellationToken) =>
        await AdvanceUsedAsync(db, salesReturn.CustomerId, salesReturn.AdvanceAmount, cancellationToken) ??
        await StockPosting.CancelAsync(
            db, current, SalesMenu.SalesReturn, salesReturn.Id, salesReturn.ReturnDate, cancellationToken);

    public static async Task<ProblemHttpResult?> CollectionAsync(
        ErpDbContext db, ICurrentContext current, SalesCollection collection, CancellationToken cancellationToken) =>
        await AdvanceUsedAsync(db, collection.CustomerId, collection.IsAdvance ? collection.CollectionAmount : 0, cancellationToken) ??
        await StockPosting.CancelAsync(
            db, current, SalesMenu.SalesCollection, collection.Id, collection.CollectionDate, cancellationToken);

    private static async Task<ProblemHttpResult?> AdvanceUsedAsync(
        ErpDbContext db, int customerId, decimal advance, CancellationToken cancellationToken) =>
        advance > 0 && await CustomerAdvances.AvailableAsync(db, customerId, 0, cancellationToken) < advance
            ? ApprovalFlow.InUse("Other collections already use this advance. Cancel them first.")
            : null;

    private static ProblemHttpResult? Used(string what, List<string> codes) =>
        codes.Count == 0 ? null : ApprovalFlow.InUse($"It has {what}: {string.Join(", ", codes)}.");
}
