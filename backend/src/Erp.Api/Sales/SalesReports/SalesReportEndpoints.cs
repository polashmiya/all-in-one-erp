using System.Globalization;
using Erp.Api.Documents;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesReports;

public sealed record SalesReportRow
{
    public required string Key { get; init; }
    public required int? Id { get; init; }
    public required string Code { get; init; }
    public required string? Name { get; init; }
    public required DateOnly? Date { get; init; }
    public required int Deliveries { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal ReturnedQuantity { get; init; }
    public required decimal GrossAmount { get; init; }
    public required decimal Discount { get; init; }
    public required decimal SdAmount { get; init; }
    public required decimal VatAmount { get; init; }
    public required decimal NetAmount { get; init; }
    public required decimal ReturnAmount { get; init; }
    public required decimal SalesAmount { get; init; }
    public required decimal Cost { get; init; }
    public required decimal Profit { get; init; }
    public required decimal? Received { get; init; }
    public required decimal? Due { get; init; }
}

public sealed record SalesReportTotals(
    int Deliveries,
    decimal Quantity,
    decimal ReturnedQuantity,
    decimal NetAmount,
    decimal ReturnAmount,
    decimal SalesAmount,
    decimal Cost,
    decimal Profit,
    decimal? Received,
    decimal? Due);

public sealed record SalesReport(IReadOnlyList<SalesReportRow> Items, int Total, SalesReportTotals Totals);

internal static class SalesReportEndpoints
{
    private const string ByItem = "item";
    private const string ByCategory = "category";
    private const string ByCustomer = "customer";
    private const string BySalesForce = "salesForce";
    private const string ByMonth = "month";

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/sales-report", Report)
            .WithName("GetSalesReport")
            .WithSummary("Approved deliveries with returns, cost, profit, receipts and due, by delivery, item, category, customer, sales force or month")
            .RequirePermission($"{SalesMenu.SalesReport}.view");
    }

    private static async Task<SalesReport> Report(
        string? view,
        string? search,
        string? type,
        int? customerId,
        Guid? warehouseId,
        int? salesForceId,
        Guid? officeId,
        DateOnly? from,
        DateOnly? to,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var deliveries = db.SalesDeliveries.AsNoTracking().Where(d => d.IsActive && d.Status == ApprovalStatus.Approved);
        if (Enum.TryParse<DeliveryType>(type, true, out var only)) deliveries = deliveries.Where(d => d.Type == only);
        if (customerId is not null) deliveries = deliveries.Where(d => d.CustomerId == customerId);
        if (warehouseId is not null) deliveries = deliveries.Where(d => d.WarehouseId == warehouseId);
        if (salesForceId is not null) deliveries = deliveries.Where(d => d.SalesForceId == salesForceId);
        if (officeId is not null) deliveries = deliveries.Where(d => d.OfficeId == officeId);
        if (from is not null) deliveries = deliveries.Where(d => d.DeliveryDate >= from);
        if (to is not null) deliveries = deliveries.Where(d => d.DeliveryDate <= to);

        var rows = view is ByItem or ByCategory
            ? await ItemRowsAsync(db, deliveries, view == ByCategory, cancellationToken)
            : Grouped(view, await DeliveryRowsAsync(db, deliveries, cancellationToken));
        if (DocumentData.Trimmed(search) is { } term)
        {
            rows = [.. rows.Where(r => r.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)];
        }

        var withReceipts = view is not (ByItem or ByCategory);
        var totals = new SalesReportTotals(
            rows.Sum(r => r.Deliveries),
            rows.Sum(r => r.Quantity),
            rows.Sum(r => r.ReturnedQuantity),
            rows.Sum(r => r.NetAmount),
            rows.Sum(r => r.ReturnAmount),
            rows.Sum(r => r.SalesAmount),
            rows.Sum(r => r.Cost),
            rows.Sum(r => r.Profit),
            withReceipts ? rows.Sum(r => r.Received) : null,
            withReceipts ? rows.Sum(r => r.Due) : null);
        return new SalesReport([.. DocumentData.Page(rows.AsQueryable(), page, pageSize)], rows.Count, totals);
    }

    private sealed record DeliveryRow(int CustomerId, int? SalesForceId, string? SalesForceName, DateOnly Date, SalesReportRow Row);

    private static async Task<List<DeliveryRow>> DeliveryRowsAsync(
        ErpDbContext db, IQueryable<SalesDelivery> deliveries, CancellationToken cancellationToken)
    {
        var rows = await deliveries
            .OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.Id)
            .Select(d => new
            {
                Delivery = d,
                Returned = d.Lines.Sum(l => l.ReturnedQuantity),
                Cost = d.Lines.Sum(l => l.CostTotal),
            })
            .ToListAsync(cancellationToken);
        var returns = await Approved(db)
            .Where(r => deliveries.Any(d => d.Id == r.DeliveryId))
            .GroupBy(r => r.DeliveryId)
            .Select(g => new
            {
                g.Key,
                Net = g.Sum(r => r.NetAmount),
                Tax = g.Sum(r => r.TotalVat + r.TotalSd),
                Cost = g.Sum(r => r.TotalCost - r.LossAmount),
            })
            .ToDictionaryAsync(g => g.Key, cancellationToken);

        return [.. rows.Select(x =>
        {
            var d = x.Delivery;
            var back = returns.GetValueOrDefault(d.Id);
            var sales = d.NetAmount - (back?.Net ?? 0);
            var cost = x.Cost - (back?.Cost ?? 0);
            return new DeliveryRow(d.CustomerId, d.SalesForceId, d.SalesForceName, d.DeliveryDate, new SalesReportRow
            {
                Key = d.Id.ToString(CultureInfo.InvariantCulture),
                Id = d.Id,
                Code = d.Code,
                Name = d.CustomerName,
                Date = d.DeliveryDate,
                Deliveries = 1,
                Quantity = d.TotalQuantity,
                ReturnedQuantity = x.Returned,
                GrossAmount = d.ItemTotalAmount,
                Discount = d.NetDiscount + d.DeductionValue,
                SdAmount = d.TotalSd,
                VatAmount = d.TotalVat,
                NetAmount = d.NetAmount,
                ReturnAmount = back?.Net ?? 0,
                SalesAmount = sales,
                Cost = cost,
                Profit = sales - (d.TotalVat + d.TotalSd - (back?.Tax ?? 0)) - cost,
                Received = d.AdvanceBalanceAdjust + d.ReceiveAmount + d.CollectedAmount,
                Due = d.PendingAmount,
            });
        })];
    }

    private static List<SalesReportRow> Grouped(string? view, List<DeliveryRow> rows) => view switch
    {
        ByCustomer => Group(rows, r => r.CustomerId.ToString(CultureInfo.InvariantCulture), r => r.Row.Name),
        BySalesForce => Group(rows, r => $"{r.SalesForceId}", r => r.SalesForceName),
        ByMonth => [.. Group(rows, r => r.Date.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            r => r.Date.ToString("MMMM yyyy", CultureInfo.InvariantCulture)).OrderByDescending(r => r.Key)],
        _ => [.. rows.Select(r => r.Row)],
    };

    private static List<SalesReportRow> Group(List<DeliveryRow> rows, Func<DeliveryRow, string> key, Func<DeliveryRow, string?> name) =>
        [.. rows.GroupBy(key)
            .Select(g => Sum(g.Key, int.TryParse(g.Key, out var id) ? id : null, "", name(g.First()), [.. g.Select(r => r.Row)]))
            .OrderByDescending(r => r.SalesAmount)];

    private static SalesReportRow Sum(string key, int? id, string code, string? name, List<SalesReportRow> rows) => new()
    {
        Key = key,
        Id = id,
        Code = code,
        Name = name,
        Date = null,
        Deliveries = rows.Sum(r => r.Deliveries),
        Quantity = rows.Sum(r => r.Quantity),
        ReturnedQuantity = rows.Sum(r => r.ReturnedQuantity),
        GrossAmount = rows.Sum(r => r.GrossAmount),
        Discount = rows.Sum(r => r.Discount),
        SdAmount = rows.Sum(r => r.SdAmount),
        VatAmount = rows.Sum(r => r.VatAmount),
        NetAmount = rows.Sum(r => r.NetAmount),
        ReturnAmount = rows.Sum(r => r.ReturnAmount),
        SalesAmount = rows.Sum(r => r.SalesAmount),
        Cost = rows.Sum(r => r.Cost),
        Profit = rows.Sum(r => r.Profit),
        Received = rows.Sum(r => r.Received),
        Due = rows.Sum(r => r.Due),
    };

    private static async Task<List<SalesReportRow>> ItemRowsAsync(
        ErpDbContext db, IQueryable<SalesDelivery> deliveries, bool byCategory, CancellationToken cancellationToken)
    {
        var lines = await deliveries.SelectMany(d => d.Lines)
            .Select(l => new
            {
                l.DeliveryId,
                l.ItemId,
                l.Item!.Code,
                l.Item.Name,
                l.Item.CategoryId,
                Category = l.Item.Category!.Name,
                l.Quantity,
                l.ReturnedQuantity,
                l.TotalAmount,
                l.DiscountAmount,
                l.SdAmount,
                l.VatAmount,
                l.TotalValue,
                l.CostTotal,
            })
            .ToListAsync(cancellationToken);
        var returned = (await Approved(db)
                .Where(r => deliveries.Any(d => d.Id == r.DeliveryId))
                .SelectMany(r => r.Lines)
                .Select(l => new { l.ItemId, l.Item!.CategoryId, l.TotalValue, Tax = l.VatAmount + l.SdAmount, Cost = l.CostTotal - l.LossAmount })
                .ToListAsync(cancellationToken))
            .ToLookup(l => byCategory ? l.CategoryId : l.ItemId);

        return [.. lines.GroupBy(l => byCategory ? l.CategoryId : l.ItemId)
            .Select(g =>
            {
                var first = g.First();
                var back = returned[g.Key].ToList();
                var sales = g.Sum(l => l.TotalValue) - back.Sum(l => l.TotalValue);
                var cost = g.Sum(l => l.CostTotal) - back.Sum(l => l.Cost);
                return new SalesReportRow
                {
                    Key = g.Key.ToString(CultureInfo.InvariantCulture),
                    Id = g.Key,
                    Code = byCategory ? "" : first.Code,
                    Name = byCategory ? first.Category : first.Name,
                    Date = null,
                    Deliveries = g.Select(l => l.DeliveryId).Distinct().Count(),
                    Quantity = g.Sum(l => l.Quantity),
                    ReturnedQuantity = g.Sum(l => l.ReturnedQuantity),
                    GrossAmount = g.Sum(l => l.TotalAmount),
                    Discount = g.Sum(l => l.DiscountAmount),
                    SdAmount = g.Sum(l => l.SdAmount),
                    VatAmount = g.Sum(l => l.VatAmount),
                    NetAmount = g.Sum(l => l.TotalValue),
                    ReturnAmount = back.Sum(l => l.TotalValue),
                    SalesAmount = sales,
                    Cost = cost,
                    Profit = sales - (g.Sum(l => l.SdAmount + l.VatAmount) - back.Sum(l => l.Tax)) - cost,
                    Received = null,
                    Due = null,
                };
            })
            .OrderByDescending(r => r.SalesAmount)];
    }

    private static IQueryable<SalesReturn> Approved(ErpDbContext db) =>
        db.SalesReturns.AsNoTracking().Where(r => r.IsActive && r.Status == ApprovalStatus.Approved);
}
