using Erp.Api.Documents;
using Erp.Api.Sales;
using Erp.Core.Authorization;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Inventory.StockReports;

public sealed record StockBalanceRow(
    int ItemId,
    string ItemCode,
    string ItemName,
    string CategoryName,
    Guid WarehouseId,
    string WarehouseName,
    string UomName,
    decimal Quantity,
    decimal Cost,
    decimal Value);

public sealed record StockBalanceCounts(int All, int InStock, int OutOfStock);

public sealed record StockBalanceReport(
    IReadOnlyList<StockBalanceRow> Items, int Total, StockBalanceCounts Counts, decimal TotalQuantity, decimal TotalValue);

public sealed record StockLedgerRow(
    int Id,
    DateOnly Date,
    string Feature,
    int DocumentId,
    string DocumentCode,
    string WarehouseName,
    string? SupplierName,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal Value,
    decimal BalanceQuantity,
    decimal BalanceValue);

public sealed record StockLedgerReport(
    string? ItemCode,
    string? ItemName,
    IReadOnlyList<StockLedgerRow> Items,
    int Total,
    decimal OpeningQuantity,
    decimal OpeningValue,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal ClosingQuantity,
    decimal ClosingValue);

internal static class StockReportEndpoints
{
    private const string InStock = "instock";
    private const string OutOfStock = "outofstock";

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/on-hand", OnHand)
            .WithName("GetStockOnHand")
            .WithSummary("Quantity and average cost of the given items in a warehouse")
            .RequireAnyPermission([.. StockModule.Editors, .. SalesModule.Editors]);

        group.MapGet("/stock-balance", Balance)
            .WithName("GetStockBalance")
            .WithSummary("Current quantity and value of every item by warehouse")
            .RequirePermission($"{InventoryMenu.StockBalance}.view");

        group.MapGet("/stock-ledger", Ledger)
            .WithName("GetStockLedger")
            .WithSummary("Stock in and out of one item with the running balance")
            .RequirePermission($"{InventoryMenu.StockLedger}.view");
    }

    private static async Task<List<StockOnHand>> OnHand(
        Guid warehouseId, int[] itemIds, ErpDbContext db, CancellationToken cancellationToken) =>
        [.. (await StockData.OnHandAsync(db, warehouseId, itemIds, cancellationToken)).Values];

    private static async Task<StockBalanceReport> Balance(
        string? search,
        string? view,
        Guid? warehouseId,
        int? categoryId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var balances = db.StockBalances.AsNoTracking();
        if (DocumentData.Trimmed(search) is { } term)
        {
            balances = balances.Where(b => b.Item!.Code.Contains(term) || b.Item.Name.Contains(term) || b.Item.BarCode.Contains(term));
        }

        if (warehouseId is not null) balances = balances.Where(b => b.WarehouseId == warehouseId);
        if (categoryId is not null) balances = balances.Where(b => b.Item!.CategoryId == categoryId);

        var all = await balances.CountAsync(cancellationToken);
        var inStock = await balances.CountAsync(b => b.Quantity > 0, cancellationToken);
        var filtered = view?.ToLowerInvariant() switch
        {
            InStock => balances.Where(b => b.Quantity > 0),
            OutOfStock => balances.Where(b => b.Quantity <= 0),
            _ => balances,
        };
        var totals = await filtered.GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Quantity = g.Sum(b => b.Quantity), Value = g.Sum(b => b.Value) })
            .FirstOrDefaultAsync(cancellationToken);
        var sorted = sort switch
        {
            "-item" => filtered.OrderByDescending(b => b.Item!.Name),
            "quantity" => filtered.OrderBy(b => b.Quantity),
            "-quantity" => filtered.OrderByDescending(b => b.Quantity),
            "value" => filtered.OrderBy(b => b.Value),
            "-value" => filtered.OrderByDescending(b => b.Value),
            _ => filtered.OrderBy(b => b.Item!.Name),
        };

        var rows = await DocumentData.Page(sorted.ThenBy(b => b.Id), page, pageSize)
            .Select(b => new StockBalanceRow(
                b.ItemId,
                b.Item!.Code,
                b.Item.Name,
                b.Item.Category!.Name,
                b.WarehouseId,
                db.Warehouses.Where(w => w.Id == b.WarehouseId).Select(w => w.Name).First(),
                b.Item.Uom!.Name,
                b.Quantity,
                b.Quantity > 0 ? Math.Round(b.Value / b.Quantity, 4) : 0,
                b.Value))
            .ToListAsync(cancellationToken);

        return new StockBalanceReport(
            rows, totals?.Count ?? 0, new StockBalanceCounts(all, inStock, all - inStock), totals?.Quantity ?? 0, totals?.Value ?? 0);
    }

    private static async Task<StockLedgerReport> Ledger(
        int itemId,
        Guid? warehouseId,
        DateOnly? from,
        DateOnly? to,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var lines = db.InventoryTransactions.AsNoTracking()
            .SelectMany(t => t.Lines, (t, l) => new { Transaction = t, Line = l })
            .Where(x => x.Line.ItemId == itemId);
        if (warehouseId is not null) lines = lines.Where(x => x.Line.WarehouseId == warehouseId);

        var opening = from is null
            ? null
            : await lines.Where(x => x.Transaction.PostingDate < from).GroupBy(_ => 1)
                .Select(g => new { Quantity = g.Sum(x => x.Line.Quantity), Value = g.Sum(x => x.Line.Value) })
                .FirstOrDefaultAsync(cancellationToken);
        if (from is not null) lines = lines.Where(x => x.Transaction.PostingDate >= from);
        if (to is not null) lines = lines.Where(x => x.Transaction.PostingDate <= to);

        var entries = await lines
            .OrderBy(x => x.Transaction.PostingDate).ThenBy(x => x.Transaction.Id).ThenBy(x => x.Line.Sequence)
            .Select(x => new
            {
                x.Line.Id,
                x.Transaction.PostingDate,
                x.Transaction.Feature,
                x.Transaction.DocumentId,
                x.Transaction.DocumentCode,
                WarehouseName = db.Warehouses.Where(w => w.Id == x.Line.WarehouseId).Select(w => w.Name).First(),
                SupplierName = db.Suppliers.Where(s => s.Id == x.Transaction.SupplierId).Select(s => s.Name).FirstOrDefault(),
                x.Line.Quantity,
                x.Line.Value,
            })
            .ToListAsync(cancellationToken);

        var quantity = opening?.Quantity ?? 0;
        var value = opening?.Value ?? 0;
        var rows = entries.Select(e =>
        {
            quantity += e.Quantity;
            value += e.Value;
            return new StockLedgerRow(
                e.Id, e.PostingDate, e.Feature, e.DocumentId, e.DocumentCode, e.WarehouseName, e.SupplierName,
                Math.Max(0, e.Quantity), Math.Max(0, -e.Quantity), e.Value, quantity, value);
        }).ToList();

        var item = await db.Items.Where(i => i.Id == itemId).Select(i => new { i.Code, i.Name }).FirstOrDefaultAsync(cancellationToken);
        return new StockLedgerReport(
            item?.Code,
            item?.Name,
            [.. DocumentData.Page(rows.AsQueryable(), page, pageSize)],
            rows.Count,
            opening?.Quantity ?? 0,
            opening?.Value ?? 0,
            rows.Sum(r => r.QuantityIn),
            rows.Sum(r => r.QuantityOut),
            quantity,
            value);
    }
}
