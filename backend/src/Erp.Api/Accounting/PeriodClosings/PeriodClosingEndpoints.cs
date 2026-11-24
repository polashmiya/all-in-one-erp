using Erp.Core.Authorization;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Approvals;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting.PeriodClosings;

public sealed record ClosingMonth(
    DateOnly Start, DateOnly End, bool IsClosed, int Pending, bool CanClose, bool CanReopen, string? ClosedByName, DateTime? ClosedAt);

public sealed record ClosingYear(
    DateOnly Start,
    DateOnly End,
    bool IsClosed,
    bool CanClose,
    bool CanReopen,
    string? ClosedByName,
    DateTime? ClosedAt,
    decimal Profit,
    IReadOnlyList<ClosingMonth> Months);

public sealed record PeriodClosingReport(int FiscalYearStartMonth, bool CanChangeStart, DateOnly? ClosedThrough, IReadOnlyList<ClosingYear> Years);

public sealed record PeriodClosingRequest(DateOnly PeriodEnd, bool IsYear);

public sealed record FiscalYearRequest(int StartMonth);

internal sealed class FiscalYearRequestValidator : AbstractValidator<FiscalYearRequest>
{
    public FiscalYearRequestValidator()
    {
        RuleFor(x => x.StartMonth).InclusiveBetween(1, 12);
    }
}

internal static class PeriodClosingEndpoints
{
    private const string Feature = AccountingMenu.PeriodClosing;
    private const int MonthsPerYear = 12;

    public static void Map(IEndpointRouteBuilder group)
    {
        var periods = group.MapGroup("/periods");

        periods.MapGet("", Report)
            .WithName("GetPeriodClosings")
            .WithSummary("Fiscal years and months of the selected business unit and whether they are closed")
            .RequirePermission($"{Feature}.view");

        periods.MapPost("/close", Close)
            .WithName("ClosePeriod")
            .WithSummary("Close a month so nothing more is posted in it, or a year and move its profit to retained earnings")
            .RequirePermission($"{Feature}.{FeatureActions.Edit}")
            .ProducesProblem(StatusCodes.Status409Conflict);

        periods.MapPost("/reopen", Reopen)
            .WithName("ReopenPeriod")
            .WithSummary("Reopen the latest closed month or year")
            .RequirePermission($"{Feature}.{FeatureActions.Edit}")
            .ProducesProblem(StatusCodes.Status409Conflict);

        periods.MapPut("/fiscal-year", SetFiscalYear)
            .WithName("SetFiscalYearStart")
            .WithSummary("The month a fiscal year starts in; it cannot change once a year is closed")
            .RequirePermission($"{Feature}.{FeatureActions.Edit}")
            .Validate<FiscalYearRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<PeriodClosingReport> Report(ErpDbContext db, CancellationToken cancellationToken)
    {
        var startMonth = await StartMonthAsync(db, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var first = await db.Journals.Where(j => j.IsActive).MinAsync(j => (DateOnly?)j.JournalDate, cancellationToken) ?? today;
        var closings = await db.PeriodClosings.AsNoTracking().Where(p => p.IsActive).ToListAsync(cancellationToken);
        var users = closings.Select(c => c.CreatedById).OfType<Guid>().ToList();
        var names = await db.ErpUsers.Where(u => users.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
        var pending = await PendingDatesAsync(db, cancellationToken);
        var profits = await ProfitLines(db)
            .GroupBy(l => l.Journal!.JournalDate)
            .Select(g => new { g.Key, Amount = g.Sum(l => l.Amount) })
            .ToListAsync(cancellationToken);
        var closedThrough = closings.Where(c => !c.IsYear).Max(c => (DateOnly?)c.PeriodEnd);
        var lastYear = closings.Where(c => c.IsYear).Max(c => (DateOnly?)c.PeriodEnd);
        string? Name(PeriodClosing? closing) => closing?.CreatedById is { } id ? names.GetValueOrDefault(id) : null;

        var years = new List<ClosingYear>();
        var firstOpenSeen = false;
        for (var start = YearStart(first, startMonth); start <= today; start = start.AddYears(1))
        {
            var end = start.AddYears(1).AddDays(-1);
            var yearClosing = closings.Find(c => c.IsYear && c.PeriodEnd == end);
            var months = new List<ClosingMonth>();
            for (var m = 0; m < MonthsPerYear; m++)
            {
                var monthStart = start.AddMonths(m);
                if (monthStart > today) break;
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var closing = closings.Find(c => !c.IsYear && c.PeriodEnd == monthEnd);
                var waiting = pending.Count(d => d >= monthStart && d <= monthEnd);
                var canClose = closing is null && !firstOpenSeen && monthEnd <= today && waiting == 0;
                if (closing is null) firstOpenSeen = true;
                months.Add(new ClosingMonth(
                    monthStart, monthEnd, closing is not null, waiting, canClose,
                    closing is not null && monthEnd == closedThrough && !(lastYear >= monthEnd),
                    Name(closing), closing?.CreatedAt));
            }

            years.Add(new ClosingYear(
                start, end, yearClosing is not null,
                yearClosing is null && months.Count == MonthsPerYear && months.All(m => m.IsClosed),
                yearClosing is not null && end == lastYear && closedThrough == end,
                Name(yearClosing), yearClosing?.CreatedAt,
                -profits.Where(p => p.Key >= start && p.Key <= end).Sum(p => p.Amount),
                months));
        }

        years.Reverse();
        return new PeriodClosingReport(startMonth, lastYear is null, closedThrough, years);
    }

    private static async Task<Results<Ok<PeriodClosingReport>, ProblemHttpResult>> Close(
        PeriodClosingRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var report = await Report(db, cancellationToken);
        var year = report.Years.FirstOrDefault(y => request.PeriodEnd >= y.Start && request.PeriodEnd <= y.End);
        var month = year?.Months.FirstOrDefault(m => m.End == request.PeriodEnd);
        if (request.IsYear ? year is not { CanClose: true } || year.End != request.PeriodEnd : month is not { CanClose: true })
        {
            return Conflict(request.IsYear
                ? "Close every month of the year first."
                : month is { Pending: > 0 }
                    ? $"{month.Pending} pending records are dated in this month. Approve or reject them first."
                    : "Close the earlier months first. A month can be closed when it is over.");
        }

        var closing = new PeriodClosing
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            PeriodStart = request.IsYear ? year!.Start : month!.Start,
            PeriodEnd = request.PeriodEnd,
            IsYear = request.IsYear,
        };
        db.PeriodClosings.Add(closing);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (request.IsYear)
        {
            var sums = await PostedLines(db)
                .Where(l => l.Journal!.JournalDate <= closing.PeriodEnd)
                .GroupBy(l => l.ChartOfAccountId)
                .Select(g => new { g.Key, Amount = g.Sum(l => l.Amount) })
                .ToListAsync(cancellationToken);
            var label = $"{closing.PeriodStart:yyyy}-{closing.PeriodEnd:yyyy}";
            var (_, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
                Feature, closing.Id, label, closing.PeriodEnd, $"Year closed {label}",
                [
                    .. sums.Select(s => JournalEntryLine.Account(s.Key, -s.Amount)),
                    JournalEntryLine.Debit(PostingKeys.RetainedEarnings, sums.Sum(s => s.Amount)),
                ]) { IsClosing = true }, cancellationToken);
            if (problem is not null) return problem;
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(await Report(db, cancellationToken));
    }

    private static async Task<Results<Ok<PeriodClosingReport>, ProblemHttpResult>> Reopen(
        PeriodClosingRequest request, ErpDbContext db, CancellationToken cancellationToken)
    {
        var report = await Report(db, cancellationToken);
        var canReopen = request.IsYear
            ? report.Years.Any(y => y.End == request.PeriodEnd && y.CanReopen)
            : report.Years.SelectMany(y => y.Months).Any(m => m.End == request.PeriodEnd && m.CanReopen);
        if (!canReopen) return Conflict("Only the latest closed month or year can be reopened. Reopen the year before its months.");

        var closing = await db.PeriodClosings.SingleAsync(
            p => p.IsActive && p.IsYear == request.IsYear && p.PeriodEnd == request.PeriodEnd, cancellationToken);
        closing.IsActive = false;
        var journal = await db.Journals.SingleOrDefaultAsync(j => j.Feature == Feature && j.DocumentId == closing.Id, cancellationToken);
        if (journal is not null) journal.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(await Report(db, cancellationToken));
    }

    private static async Task<Results<Ok<PeriodClosingReport>, ProblemHttpResult>> SetFiscalYear(
        FiscalYearRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (await db.PeriodClosings.AcrossTenants()
            .AnyAsync(p => p.AccountId == current.AccountId && p.IsActive && p.IsYear, cancellationToken))
        {
            return Conflict("A year is already closed, so the start of the fiscal year cannot change.");
        }

        var setting = await db.AccountingSettings.SingleOrDefaultAsync(cancellationToken);
        if (setting is null)
        {
            setting = new AccountingSetting { AccountId = current.AccountId!.Value };
            db.AccountingSettings.Add(setting);
        }

        setting.FiscalYearStartMonth = request.StartMonth;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(await Report(db, cancellationToken));
    }

    private static ProblemHttpResult Conflict(string detail) =>
        TypedResults.Problem(title: "Period not changed", detail: detail, statusCode: StatusCodes.Status409Conflict);

    private static async Task<int> StartMonthAsync(ErpDbContext db, CancellationToken cancellationToken) =>
        await db.AccountingSettings.Select(s => (int?)s.FiscalYearStartMonth).SingleOrDefaultAsync(cancellationToken)
        ?? AccountingSetting.DefaultFiscalYearStartMonth;

    private static DateOnly YearStart(DateOnly date, int startMonth) =>
        new(date.Month >= startMonth ? date.Year : date.Year - 1, startMonth, 1);

    private static IQueryable<JournalLine> PostedLines(ErpDbContext db) =>
        db.Journals.AsNoTracking().Posted().SelectMany(j => j.Lines).Where(l => l.ChartOfAccount!.Type >= AccountType.Income);

    private static IQueryable<JournalLine> ProfitLines(ErpDbContext db) => PostedLines(db).Where(l => l.Journal!.Feature != Feature);

    private static async Task<List<DateOnly>> PendingDatesAsync(ErpDbContext db, CancellationToken cancellationToken) =>
    [
        .. await db.PurchaseReceives.Where(d => d.IsActive && d.Status == ApprovalStatus.Pending).Select(d => d.ReceiveDate).ToListAsync(cancellationToken),
        .. await db.PurchaseReturns.Where(d => d.IsActive && d.Status == ApprovalStatus.Pending).Select(d => d.ReturnDate).ToListAsync(cancellationToken),
        .. await db.PurchasePayments.Where(d => d.IsActive && d.Status == ApprovalStatus.Pending).Select(d => d.PaymentDate).ToListAsync(cancellationToken),
        .. await db.StockAdjustments.Where(d => d.IsActive && d.Status == ApprovalStatus.Pending).Select(d => d.AdjustmentDate).ToListAsync(cancellationToken),
        .. await db.StockTransfers.Where(d => d.IsActive && d.Status == ApprovalStatus.Pending).Select(d => d.TransferDate).ToListAsync(cancellationToken),
        .. await db.Journals.Where(d => d.IsActive && d.Status == ApprovalStatus.Pending).Select(d => d.JournalDate).ToListAsync(cancellationToken),
        .. await db.TaxDeposits.Where(d => d.IsActive && d.Status == ApprovalStatus.Pending).Select(d => d.DepositDate).ToListAsync(cancellationToken),
    ];
}
