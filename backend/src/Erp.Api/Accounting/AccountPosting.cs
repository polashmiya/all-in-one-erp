using Erp.Api.Documents;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Approvals;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting;

internal sealed record JournalEntry(
    string Feature, int DocumentId, string DocumentCode, DateOnly Date, string Narration, IReadOnlyList<JournalEntryLine> Lines)
{
    public bool IsReversal { get; init; }

    public bool IsClosing { get; init; }
}

internal sealed record JournalEntryLine(string? Key, int? ChartOfAccountId, decimal Amount, int? SupplierId = null)
{
    public static JournalEntryLine Debit(string key, decimal amount, int? supplierId = null) => new(key, null, amount, supplierId);

    public static JournalEntryLine Credit(string key, decimal amount, int? supplierId = null) => new(key, null, -amount, supplierId);

    public static JournalEntryLine Account(int chartOfAccountId, decimal amount, int? supplierId = null) =>
        new(null, chartOfAccountId, amount, supplierId);
}

internal static class AccountPosting
{
    public const string CodePrefix = "JV-";

    public static async Task<(Journal? Journal, ProblemHttpResult? Problem)> PostAsync(
        ErpDbContext db, ICurrentContext current, JournalEntry entry, CancellationToken cancellationToken)
    {
        if (entry.Lines.Sum(l => l.Amount) != 0)
        {
            throw new InvalidOperationException($"The journal of {entry.DocumentCode} does not balance.");
        }

        var wanted = entry.Lines.Where(l => l.Amount != 0).ToList();
        var keys = wanted.Select(l => l.Key).OfType<string>().Distinct().ToList();
        var accounts = await db.PostingAccounts
            .Where(p => keys.Contains(p.Key) && p.ChartOfAccount!.IsActive && !p.ChartOfAccount.IsGroup)
            .ToDictionaryAsync(p => p.Key, p => p.ChartOfAccountId, cancellationToken);
        var missing = keys.Where(k => !accounts.ContainsKey(k)).ToList();
        if (missing.Count > 0)
        {
            return (null, TypedResults.Problem(
                title: "Posting account missing",
                detail: $"Choose an active account for {string.Join(", ", missing)} in Accounting, Posting Accounts.",
                statusCode: StatusCodes.Status409Conflict));
        }

        var lines = wanted
            .GroupBy(l => (AccountId: l.ChartOfAccountId ?? accounts[l.Key!], l.SupplierId))
            .Select(g => (g.Key.AccountId, g.Key.SupplierId, Amount: g.Sum(l => l.Amount)))
            .Where(l => l.Amount != 0)
            .OrderByDescending(l => l.Amount > 0)
            .ToList();
        if (lines.Count == 0) return (null, null);
        if (!entry.IsClosing && await PeriodLock.ProblemAsync(db, entry.Date, cancellationToken) is { } closed) return (null, closed);

        var journal = new Journal
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = await DocumentData.NextCodeAsync(db.Journals, CodePrefix, cancellationToken),
            JournalDate = entry.Date,
            Narration = entry.Narration,
            Feature = entry.Feature,
            DocumentId = entry.DocumentId,
            DocumentCode = entry.DocumentCode,
            IsReversal = entry.IsReversal,
            TotalAmount = lines.Where(l => l.Amount > 0).Sum(l => l.Amount),
            Status = ApprovalStatus.Approved,
            ReviewedAt = DateTime.UtcNow,
            ReviewedById = current.UserId,
        };
        journal.Lines.AddRange(lines.Select((l, index) => new JournalLine
        {
            Sequence = index + 1,
            ChartOfAccountId = l.AccountId,
            SupplierId = l.SupplierId,
            Amount = l.Amount,
        }));
        db.Journals.Add(journal);
        return (journal, null);
    }

    public static async Task<(Journal? Journal, ProblemHttpResult? Problem)> ReverseAsync(
        ErpDbContext db, ICurrentContext current, string feature, int documentId, DateOnly date, decimal stockDifference,
        CancellationToken cancellationToken)
    {
        var original = await db.Journals.AsNoTracking().Include(j => j.Lines)
            .SingleOrDefaultAsync(j => j.Feature == feature && j.DocumentId == documentId && !j.IsReversal, cancellationToken);
        return original is null
            ? (null, null)
            : await ReverseAsync(db, current, original, feature, documentId, original.DocumentCode!, date, stockDifference, cancellationToken);
    }

    public static Task<(Journal? Journal, ProblemHttpResult? Problem)> ReverseAsync(
        ErpDbContext db, ICurrentContext current, Journal original, string feature, int documentId, string documentCode, DateOnly date,
        decimal stockDifference, CancellationToken cancellationToken) =>
        PostAsync(db, current, new JournalEntry(
            feature, documentId, documentCode, date, $"Cancelled {documentCode}",
            [
                .. original.Lines.OrderBy(l => l.Sequence).Select(l => JournalEntryLine.Account(l.ChartOfAccountId, -l.Amount, l.SupplierId)),
                JournalEntryLine.Debit(PostingKeys.Inventory, stockDifference),
                JournalEntryLine.Credit(PostingKeys.PriceDifference, stockDifference),
            ]) { IsReversal = true }, cancellationToken);
}
