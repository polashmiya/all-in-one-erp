using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Api.Documents;
using Erp.Core.Authorization;
using Erp.Core.Domain.Accounting;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting.AccountingReports;

public sealed record LedgerRow(
    int Id,
    DateOnly Date,
    int JournalId,
    string JournalCode,
    string Narration,
    string? Feature,
    int? DocumentId,
    string? DocumentCode,
    string AccountName,
    string? SupplierName,
    decimal Debit,
    decimal Credit,
    decimal Balance);

public sealed record LedgerReport(
    IReadOnlyList<LedgerRow> Items, int Total, decimal Opening, decimal Debit, decimal Credit, decimal Closing);

public sealed record TrialBalanceRow(
    int Id, string Code, string Name, string Type, decimal Opening, decimal Debit, decimal Credit, decimal Closing);

public sealed record TrialBalanceReport(
    IReadOnlyList<TrialBalanceRow> Items, int Total, decimal Debit, decimal Credit, decimal ClosingDebit, decimal ClosingCredit);

public sealed record StatementLine(int Id, string Code, string Name, decimal Amount);

public sealed record StatementGroup(string Type, IReadOnlyList<StatementLine> Lines, decimal Total);

public sealed record IncomeStatement(
    StatementGroup Income, StatementGroup CostOfSales, decimal GrossProfit, StatementGroup Expenses, decimal NetProfit);

public sealed record BalanceSheet(
    IReadOnlyList<StatementGroup> Assets,
    decimal TotalAssets,
    IReadOnlyList<StatementGroup> Liabilities,
    decimal TotalLiabilities,
    StatementGroup Equity,
    decimal CurrentEarnings,
    decimal TotalEquity);

internal static class AccountingReportEndpoints
{
    private sealed record AccountSum(int Id, string Code, string Name, AccountType Type, decimal Amount);

    public static void Map(IEndpointRouteBuilder group)
    {
        var reports = group.MapGroup("/reports");

        reports.MapGet("/account-ledger", AccountLedger)
            .WithName("GetAccountLedger")
            .WithSummary("Debits and credits of one account with the running balance")
            .RequirePermission($"{AccountingMenu.AccountLedger}.view");

        reports.MapGet("/supplier-ledger", SupplierLedger)
            .WithName("GetSupplierLedger")
            .WithSummary("Bills, returns and payments of one supplier with the running balance")
            .RequirePermission($"{AccountingMenu.SupplierLedger}.view");

        reports.MapGet("/trial-balance", TrialBalance)
            .WithName("GetTrialBalance")
            .RequirePermission($"{AccountingMenu.TrialBalance}.view");

        reports.MapGet("/income-statement", IncomeStatement)
            .WithName("GetIncomeStatement")
            .RequirePermission($"{AccountingMenu.IncomeStatement}.view");

        reports.MapGet("/balance-sheet", BalanceSheet)
            .WithName("GetBalanceSheet")
            .RequirePermission($"{AccountingMenu.BalanceSheet}.view");
    }

    private static IQueryable<JournalLine> Posted(ErpDbContext db) =>
        db.Journals.AsNoTracking().Posted().SelectMany(j => j.Lines);

    private static Task<LedgerReport> AccountLedger(
        int accountId, int? supplierId, DateOnly? from, DateOnly? to, int? page, int? pageSize, ErpDbContext db, CancellationToken cancellationToken) =>
        LedgerAsync(
            db,
            Posted(db).Where(l => l.ChartOfAccountId == accountId && (supplierId == null || l.SupplierId == supplierId)),
            from, to, page, pageSize, cancellationToken);

    private static Task<LedgerReport> SupplierLedger(
        int supplierId, DateOnly? from, DateOnly? to, int? page, int? pageSize, ErpDbContext db, CancellationToken cancellationToken) =>
        LedgerAsync(db, Posted(db).Where(l => l.SupplierId == supplierId), from, to, page, pageSize, cancellationToken);

    private static async Task<LedgerReport> LedgerAsync(
        ErpDbContext db, IQueryable<JournalLine> lines, DateOnly? from, DateOnly? to, int? page, int? pageSize,
        CancellationToken cancellationToken)
    {
        var opening = from is null
            ? 0
            : await lines.Where(l => l.Journal!.JournalDate < from).SumAsync(l => l.Amount, cancellationToken);
        if (from is not null) lines = lines.Where(l => l.Journal!.JournalDate >= from);
        if (to is not null) lines = lines.Where(l => l.Journal!.JournalDate <= to);

        var entries = await lines
            .OrderBy(l => l.Journal!.JournalDate).ThenBy(l => l.JournalId).ThenBy(l => l.Sequence)
            .Select(l => new
            {
                l.Id,
                l.Journal!.JournalDate,
                l.JournalId,
                l.Journal.Code,
                l.Journal.Narration,
                l.Journal.Feature,
                l.Journal.DocumentId,
                l.Journal.DocumentCode,
                AccountName = l.ChartOfAccount!.Name,
                SupplierName = db.Suppliers.Where(s => s.Id == l.SupplierId).Select(s => s.Name).FirstOrDefault(),
                l.Amount,
            })
            .ToListAsync(cancellationToken);

        var balance = opening;
        var rows = entries.Select(e =>
        {
            balance += e.Amount;
            return new LedgerRow(
                e.Id, e.JournalDate, e.JournalId, e.Code, e.Narration, e.Feature, e.DocumentId, e.DocumentCode, e.AccountName,
                e.SupplierName, Math.Max(0, e.Amount), Math.Max(0, -e.Amount), balance);
        }).ToList();

        return new LedgerReport(
            [.. DocumentData.Page(rows.AsQueryable(), page, pageSize)], rows.Count, opening, rows.Sum(r => r.Debit), rows.Sum(r => r.Credit), balance);
    }

    private static async Task<TrialBalanceReport> TrialBalance(
        DateOnly? from, DateOnly? to, string? search, ErpDbContext db, CancellationToken cancellationToken)
    {
        var lines = Posted(db);
        if (to is not null) lines = lines.Where(l => l.Journal!.JournalDate <= to);
        var sums = await lines
            .GroupBy(l => l.ChartOfAccountId)
            .Select(g => new
            {
                g.Key,
                Opening = g.Sum(l => from != null && l.Journal!.JournalDate < from ? l.Amount : 0),
                Debit = g.Sum(l => (from == null || l.Journal!.JournalDate >= from) && l.Amount > 0 ? l.Amount : 0),
                Credit = g.Sum(l => (from == null || l.Journal!.JournalDate >= from) && l.Amount < 0 ? -l.Amount : 0),
            })
            .ToDictionaryAsync(g => g.Key, cancellationToken);

        var accounts = db.ChartOfAccounts.AsNoTracking().Where(a => sums.Keys.Contains(a.Id));
        if (DocumentData.Trimmed(search) is { } term) accounts = accounts.Where(a => a.Code.Contains(term) || a.Name.Contains(term));
        var rows = (await accounts.OrderBy(a => a.Code).ToListAsync(cancellationToken))
            .Select(a =>
            {
                var sum = sums[a.Id];
                return new TrialBalanceRow(
                    a.Id, a.Code, a.Name, ChartOfAccountEndpoints.TypeKey(a.Type), sum.Opening, sum.Debit, sum.Credit,
                    sum.Opening + sum.Debit - sum.Credit);
            })
            .ToList();

        return new TrialBalanceReport(
            rows, rows.Count, rows.Sum(r => r.Debit), rows.Sum(r => r.Credit), rows.Sum(r => Math.Max(0, r.Closing)),
            rows.Sum(r => Math.Max(0, -r.Closing)));
    }

    private static async Task<IncomeStatement> IncomeStatement(
        DateOnly? from, DateOnly? to, ErpDbContext db, CancellationToken cancellationToken)
    {
        var sums = await SumsAsync(Posted(db).Where(l => l.Journal!.Feature != AccountingMenu.PeriodClosing), from, to, cancellationToken);
        var income = Group(sums, AccountType.Income, -1);
        var cost = Group(sums, AccountType.CostOfSales, 1);
        var expenses = Group(sums, AccountType.Expense, 1);
        var gross = income.Total - cost.Total;
        return new IncomeStatement(income, cost, gross, expenses, gross - expenses.Total);
    }

    private static async Task<BalanceSheet> BalanceSheet(DateOnly? to, ErpDbContext db, CancellationToken cancellationToken)
    {
        var sums = await SumsAsync(Posted(db), null, to, cancellationToken);
        StatementGroup[] assets = [Group(sums, AccountType.CurrentAsset, 1), Group(sums, AccountType.NonCurrentAsset, 1)];
        StatementGroup[] liabilities = [Group(sums, AccountType.CurrentLiability, -1), Group(sums, AccountType.NonCurrentLiability, -1)];
        var equity = Group(sums, AccountType.Equity, -1);
        var earnings = -sums.Where(s => s.Type >= AccountType.Income).Sum(s => s.Amount);
        return new BalanceSheet(
            assets, assets.Sum(g => g.Total), liabilities, liabilities.Sum(g => g.Total), equity, earnings, equity.Total + earnings);
    }

    private static async Task<List<AccountSum>> SumsAsync(
        IQueryable<JournalLine> lines, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (from is not null) lines = lines.Where(l => l.Journal!.JournalDate >= from);
        if (to is not null) lines = lines.Where(l => l.Journal!.JournalDate <= to);
        return await lines
            .GroupBy(l => new { l.ChartOfAccountId, l.ChartOfAccount!.Code, l.ChartOfAccount.Name, l.ChartOfAccount.Type })
            .Select(g => new AccountSum(g.Key.ChartOfAccountId, g.Key.Code, g.Key.Name, g.Key.Type, g.Sum(l => l.Amount)))
            .ToListAsync(cancellationToken);
    }

    private static StatementGroup Group(List<AccountSum> sums, AccountType type, int sign)
    {
        var lines = sums.Where(s => s.Type == type && s.Amount != 0)
            .OrderBy(s => s.Code)
            .Select(s => new StatementLine(s.Id, s.Code, s.Name, sign * s.Amount))
            .ToList();
        return new StatementGroup(ChartOfAccountEndpoints.TypeKey(type), lines, lines.Sum(l => l.Amount));
    }
}
