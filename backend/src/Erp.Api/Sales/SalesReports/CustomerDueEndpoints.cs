using Erp.Api.Documents;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesReports;

public sealed record CustomerDueRow(
    string Key,
    int Id,
    string Code,
    string? Name,
    DateOnly? Date,
    DateOnly? DueDate,
    int? OverdueDays,
    int Bills,
    decimal BillAmount,
    decimal ReturnAmount,
    decimal Received,
    decimal Due,
    decimal NotDue,
    decimal Days30,
    decimal Days60,
    decimal Days90,
    decimal Older,
    decimal? CreditLimit);

public sealed record CustomerDueTotals(
    int Bills, decimal Due, decimal NotDue, decimal Days30, decimal Days60, decimal Days90, decimal Older);

public sealed record CustomerDueReport(IReadOnlyList<CustomerDueRow> Items, int Total, CustomerDueTotals Totals);

internal static class CustomerDueEndpoints
{
    private const string ByBill = "bill";

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/customer-due", Report)
            .WithName("GetCustomerDueReport")
            .WithSummary("What customers still owe by age, by customer or by bill")
            .RequirePermission($"{SalesMenu.CustomerDue}.view");
    }

    private static async Task<CustomerDueReport> Report(
        string? view,
        string? search,
        int? customerId,
        int? salesForceId,
        bool? overdue,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var deliveries = db.SalesDeliveries.AsNoTracking()
            .Where(d => d.IsActive && d.Status == ApprovalStatus.Approved && d.PendingAmount > 0);
        if (customerId is not null) deliveries = deliveries.Where(d => d.CustomerId == customerId);
        if (salesForceId is not null) deliveries = deliveries.Where(d => d.SalesForceId == salesForceId);

        var bills = (await deliveries
                .OrderBy(d => d.DeliveryDate).ThenBy(d => d.Id)
                .Select(d => new
                {
                    d.Id,
                    d.Code,
                    d.CustomerId,
                    d.CustomerName,
                    d.DeliveryDate,
                    d.PaymentTermDays,
                    d.NetAmount,
                    d.ReturnAdjusted,
                    d.PendingAmount,
                })
                .ToListAsync(cancellationToken))
            .Select(d =>
            {
                var dueDate = d.DeliveryDate.AddDays(d.PaymentTermDays);
                var days = today.DayNumber - dueDate.DayNumber;
                return (d.CustomerId, Row: new CustomerDueRow(
                    $"{d.Id}", d.Id, d.Code, d.CustomerName, d.DeliveryDate, dueDate, Math.Max(0, days), 1, d.NetAmount, d.ReturnAdjusted,
                    d.NetAmount - d.ReturnAdjusted - d.PendingAmount, d.PendingAmount,
                    days <= 0 ? d.PendingAmount : 0,
                    days is > 0 and <= 30 ? d.PendingAmount : 0,
                    days is > 30 and <= 60 ? d.PendingAmount : 0,
                    days is > 60 and <= 90 ? d.PendingAmount : 0,
                    days > 90 ? d.PendingAmount : 0,
                    null));
            })
            .Where(b => overdue != true || b.Row.OverdueDays > 0)
            .ToList();

        var rows = view == ByBill
            ? bills.Select(b => b.Row).ToList()
            : await ByCustomerAsync(db, bills, cancellationToken);
        if (DocumentData.Trimmed(search) is { } term)
        {
            rows = [.. rows.Where(r => r.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)];
        }

        var totals = new CustomerDueTotals(
            rows.Sum(r => r.Bills), rows.Sum(r => r.Due), rows.Sum(r => r.NotDue), rows.Sum(r => r.Days30), rows.Sum(r => r.Days60),
            rows.Sum(r => r.Days90), rows.Sum(r => r.Older));
        return new CustomerDueReport([.. DocumentData.Page(rows.AsQueryable(), page, pageSize)], rows.Count, totals);
    }

    private static async Task<List<CustomerDueRow>> ByCustomerAsync(
        ErpDbContext db, List<(int CustomerId, CustomerDueRow Row)> bills, CancellationToken cancellationToken)
    {
        var ids = bills.Select(b => b.CustomerId).Distinct().ToList();
        var customers = await db.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new { c.Code, c.Name, c.CreditLimit }, cancellationToken);
        return [.. bills.GroupBy(b => b.CustomerId)
            .Select(g =>
            {
                var rows = g.Select(b => b.Row).ToList();
                var customer = customers[g.Key];
                return new CustomerDueRow(
                    $"{g.Key}", g.Key, customer.Code, customer.Name, null, null, rows.Max(r => r.OverdueDays), rows.Count,
                    rows.Sum(r => r.BillAmount), rows.Sum(r => r.ReturnAmount), rows.Sum(r => r.Received), rows.Sum(r => r.Due),
                    rows.Sum(r => r.NotDue), rows.Sum(r => r.Days30), rows.Sum(r => r.Days60), rows.Sum(r => r.Days90),
                    rows.Sum(r => r.Older), customer.CreditLimit);
            })
            .OrderByDescending(r => r.Due)];
    }
}
