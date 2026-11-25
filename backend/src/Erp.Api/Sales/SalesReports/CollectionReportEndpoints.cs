using System.Globalization;
using Erp.Api.Documents;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesReports;

public sealed record CollectionReportRow
{
    public required string Key { get; init; }
    public required int? Id { get; init; }
    public required string Code { get; init; }
    public required string? Name { get; init; }
    public required DateOnly? Date { get; init; }
    public required bool IsAdvance { get; init; }
    public required string? Method { get; init; }
    public required string? Account { get; init; }
    public required string? Instrument { get; init; }
    public required int Collections { get; init; }
    public required decimal Collected { get; init; }
    public required decimal Advance { get; init; }
    public required decimal Vds { get; init; }
    public required decimal Ait { get; init; }
    public required decimal Discount { get; init; }
    public required decimal Expense { get; init; }
    public required decimal AdvanceAdjusted { get; init; }
    public required decimal Settled { get; init; }
}

public sealed record CollectionReportTotals(
    int Collections, decimal Collected, decimal Advance, decimal Vds, decimal Ait, decimal Discount, decimal Expense, decimal AdvanceAdjusted,
    decimal Settled);

public sealed record CollectionReport(IReadOnlyList<CollectionReportRow> Items, int Total, CollectionReportTotals Totals);

internal static class CollectionReportEndpoints
{
    private const string ByCustomer = "customer";
    private const string ByMethod = "method";
    private const string ByCollector = "collector";
    private const string ByDate = "date";

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/collection-report", Report)
            .WithName("GetCollectionReport")
            .WithSummary("Approved collections by collection, customer, payment method, collector or date")
            .RequirePermission($"{SalesMenu.CollectionReport}.view");
    }

    private static async Task<CollectionReport> Report(
        string? view,
        string? search,
        int? customerId,
        int? paymentMethodId,
        int? collectedById,
        DateOnly? from,
        DateOnly? to,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.SalesCollections.AsNoTracking().Where(c => c.IsActive && c.Status == ApprovalStatus.Approved);
        if (customerId is not null) query = query.Where(c => c.CustomerId == customerId);
        if (paymentMethodId is not null) query = query.Where(c => c.PaymentMethodId == paymentMethodId);
        if (collectedById is not null) query = query.Where(c => c.CollectedById == collectedById);
        if (from is not null) query = query.Where(c => c.CollectionDate >= from);
        if (to is not null) query = query.Where(c => c.CollectionDate <= to);

        var collections = await query.OrderByDescending(c => c.CollectionDate).ThenByDescending(c => c.Id).ToListAsync(cancellationToken);
        var rows = view switch
        {
            ByCustomer => Group(collections, c => $"{c.CustomerId}", c => c.CustomerName),
            ByMethod => Group(collections, c => $"{c.PaymentMethodId}", c => c.PaymentMethodName),
            ByCollector => Group(collections, c => $"{c.CollectedById}", c => c.CollectedByName),
            ByDate => [.. Group(collections, c => c.CollectionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), _ => null)
                .Select(r => r with { Date = DateOnly.Parse(r.Key, CultureInfo.InvariantCulture) })
                .OrderByDescending(r => r.Key)],
            _ => [.. collections.Select(Single)],
        };
        if (DocumentData.Trimmed(search) is { } term)
        {
            rows = [.. rows.Where(r => r.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true ||
                r.Instrument?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)];
        }

        var totals = new CollectionReportTotals(
            rows.Sum(r => r.Collections), rows.Sum(r => r.Collected), rows.Sum(r => r.Advance), rows.Sum(r => r.Vds), rows.Sum(r => r.Ait),
            rows.Sum(r => r.Discount), rows.Sum(r => r.Expense), rows.Sum(r => r.AdvanceAdjusted), rows.Sum(r => r.Settled));
        return new CollectionReport([.. DocumentData.Page(rows.AsQueryable(), page, pageSize)], rows.Count, totals);
    }

    private static CollectionReportRow Single(SalesCollection c) => new()
    {
        Key = c.Id.ToString(CultureInfo.InvariantCulture),
        Id = c.Id,
        Code = c.Code,
        Name = c.CustomerName,
        Date = c.CollectionDate,
        IsAdvance = c.IsAdvance,
        Method = c.PaymentMethodName,
        Account = c.BankAccountName,
        Instrument = c.InstrumentNo ?? c.TransactionNo ?? c.MoneyReceiptNo,
        Collections = 1,
        Collected = c.IsAdvance ? 0 : c.CollectionAmount,
        Advance = c.IsAdvance ? c.CollectionAmount : 0,
        Vds = c.Vds,
        Ait = c.Ait,
        Discount = c.DiscountAmount,
        Expense = c.ExpenseAmount,
        AdvanceAdjusted = c.AdvanceAdjusted,
        Settled = c.SettledAmount,
    };

    private static List<CollectionReportRow> Group(
        List<SalesCollection> collections, Func<SalesCollection, string> key, Func<SalesCollection, string?> name) =>
        [.. collections.GroupBy(key)
            .Select(g =>
            {
                var rows = g.Select(Single).ToList();
                return new CollectionReportRow
                {
                    Key = g.Key,
                    Id = null,
                    Code = "",
                    Name = name(g.First()),
                    Date = null,
                    IsAdvance = false,
                    Method = null,
                    Account = null,
                    Instrument = null,
                    Collections = rows.Count,
                    Collected = rows.Sum(r => r.Collected),
                    Advance = rows.Sum(r => r.Advance),
                    Vds = rows.Sum(r => r.Vds),
                    Ait = rows.Sum(r => r.Ait),
                    Discount = rows.Sum(r => r.Discount),
                    Expense = rows.Sum(r => r.Expense),
                    AdvanceAdjusted = rows.Sum(r => r.AdvanceAdjusted),
                    Settled = rows.Sum(r => r.Settled),
                };
            })
            .OrderByDescending(r => r.Settled)];
}
