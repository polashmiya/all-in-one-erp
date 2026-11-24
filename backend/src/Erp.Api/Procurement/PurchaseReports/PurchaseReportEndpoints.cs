using Erp.Api.Documents;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Procurement;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchaseReports;

public sealed record PurchaseReportRow(
    int Id,
    string Code,
    string? Name,
    DateOnly? Date,
    int Receives,
    decimal Quantity,
    decimal ReturnedQuantity,
    decimal VatAmount,
    decimal NetAmount,
    decimal ReturnAmount,
    decimal PurchaseAmount,
    decimal? PaidAmount,
    decimal? Due);

public sealed record PurchaseReportTotals(
    decimal Quantity,
    decimal ReturnedQuantity,
    decimal VatAmount,
    decimal NetAmount,
    decimal ReturnAmount,
    decimal PurchaseAmount,
    decimal? PaidAmount,
    decimal? Due);

public sealed record PurchaseReport(IReadOnlyList<PurchaseReportRow> Items, int Total, PurchaseReportTotals Totals);

internal static class PurchaseReportEndpoints
{
    private const string ByItem = "item";
    private const string BySupplier = "supplier";

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/purchase-report", Report)
            .WithName("GetPurchaseReport")
            .WithSummary("Approved goods receives with returns, payments and due, by receive, item or supplier")
            .RequirePermission($"{ProcurementMenu.PurchaseReport}.view");
    }

    private static async Task<PurchaseReport> Report(
        string? view,
        string? search,
        int? supplierId,
        Guid? warehouseId,
        DateOnly? from,
        DateOnly? to,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var receives = db.PurchaseReceives.AsNoTracking().Where(r => r.IsActive && r.Status == ApprovalStatus.Approved);
        if (supplierId is not null) receives = receives.Where(r => r.SupplierId == supplierId);
        if (warehouseId is not null) receives = receives.Where(r => r.WarehouseId == warehouseId);
        if (from is not null) receives = receives.Where(r => r.ReceiveDate >= from);
        if (to is not null) receives = receives.Where(r => r.ReceiveDate <= to);

        var rows = view switch
        {
            ByItem => await ItemRowsAsync(db, receives, cancellationToken),
            BySupplier => await SupplierRowsAsync(db, receives, cancellationToken),
            _ => await ReceiveRowsAsync(db, receives, cancellationToken),
        };
        if (DocumentData.Trimmed(search) is { } term)
        {
            rows = [.. rows.Where(r => r.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)];
        }

        var withPayments = view != ByItem;
        var totals = new PurchaseReportTotals(
            rows.Sum(r => r.Quantity),
            rows.Sum(r => r.ReturnedQuantity),
            rows.Sum(r => r.VatAmount),
            rows.Sum(r => r.NetAmount),
            rows.Sum(r => r.ReturnAmount),
            rows.Sum(r => r.PurchaseAmount),
            withPayments ? rows.Sum(r => r.PaidAmount) : null,
            withPayments ? rows.Sum(r => r.Due) : null);
        return new PurchaseReport([.. DocumentData.Page(rows.AsQueryable(), page, pageSize)], rows.Count, totals);
    }

    private static async Task<List<PurchaseReportRow>> ReceiveRowsAsync(
        ErpDbContext db, IQueryable<PurchaseReceive> receives, CancellationToken cancellationToken)
    {
        var rows = await receives
            .OrderByDescending(r => r.ReceiveDate).ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Code,
                SupplierName = db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefault(),
                r.ReceiveDate,
                r.TotalQuantity,
                Returned = r.Lines.Sum(l => l.ReturnedQuantity),
                r.TotalVatAmount,
                r.NetAmount,
                r.ReturnAmount,
                r.PaidAmount,
            })
            .ToListAsync(cancellationToken);
        return [.. rows.Select(r => new PurchaseReportRow(
            r.Id, r.Code, r.SupplierName, r.ReceiveDate, 1, r.TotalQuantity, r.Returned, r.TotalVatAmount, r.NetAmount, r.ReturnAmount,
            r.NetAmount - r.ReturnAmount, r.PaidAmount, r.NetAmount - r.ReturnAmount - r.PaidAmount))];
    }

    private static async Task<List<PurchaseReportRow>> SupplierRowsAsync(
        ErpDbContext db, IQueryable<PurchaseReceive> receives, CancellationToken cancellationToken)
    {
        var rows = await receives
            .GroupBy(r => r.SupplierId)
            .Select(g => new
            {
                g.Key,
                Receives = g.Count(),
                Quantity = g.Sum(r => r.TotalQuantity),
                Vat = g.Sum(r => r.TotalVatAmount),
                Net = g.Sum(r => r.NetAmount),
                Return = g.Sum(r => r.ReturnAmount),
                Paid = g.Sum(r => r.PaidAmount),
            })
            .ToListAsync(cancellationToken);
        var returned = await receives.SelectMany(r => r.Lines, (r, l) => new { r.SupplierId, l.ReturnedQuantity })
            .GroupBy(x => x.SupplierId)
            .Select(g => new { g.Key, Quantity = g.Sum(x => x.ReturnedQuantity) })
            .ToDictionaryAsync(g => g.Key, g => g.Quantity, cancellationToken);
        var ids = rows.Select(r => r.Key).ToList();
        var suppliers = await db.Suppliers.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => new { s.Code, s.Name }, cancellationToken);
        return [.. rows
            .Select(r => new PurchaseReportRow(
                r.Key, suppliers[r.Key].Code, suppliers[r.Key].Name, null, r.Receives, r.Quantity, returned.GetValueOrDefault(r.Key), r.Vat,
                r.Net, r.Return, r.Net - r.Return, r.Paid, r.Net - r.Return - r.Paid))
            .OrderByDescending(r => r.PurchaseAmount)];
    }

    private static async Task<List<PurchaseReportRow>> ItemRowsAsync(
        ErpDbContext db, IQueryable<PurchaseReceive> receives, CancellationToken cancellationToken)
    {
        var rows = await receives
            .SelectMany(r => r.Lines)
            .GroupBy(l => new { l.ItemId, l.Item!.Code, l.Item.Name })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.Code,
                g.Key.Name,
                Receives = g.Select(l => l.PurchaseReceiveId).Distinct().Count(),
                Quantity = g.Sum(l => l.Quantity),
                Returned = g.Sum(l => l.ReturnedQuantity),
                Vat = g.Sum(l => l.VatAmount),
                Net = g.Sum(l => l.NetAmount),
            })
            .ToListAsync(cancellationToken);
        var returns = await db.PurchaseReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != ApprovalStatus.Rejected && r.Status != ApprovalStatus.Cancelled && receives.Any(g => g.Id == r.PurchaseReceiveId))
            .SelectMany(r => r.Lines)
            .GroupBy(l => l.ItemId)
            .Select(g => new { g.Key, Net = g.Sum(l => l.NetAmount) })
            .ToDictionaryAsync(g => g.Key, g => g.Net, cancellationToken);
        return [.. rows
            .Select(r =>
            {
                var returned = returns.GetValueOrDefault(r.ItemId);
                return new PurchaseReportRow(
                    r.ItemId, r.Code, r.Name, null, r.Receives, r.Quantity, r.Returned, r.Vat, r.Net, returned, r.Net - returned, null, null);
            })
            .OrderByDescending(r => r.PurchaseAmount)];
    }
}
