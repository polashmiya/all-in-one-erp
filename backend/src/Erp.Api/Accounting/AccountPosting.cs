using Erp.Api.Documents;
using Erp.Api.Procurement;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Approvals;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting;

internal sealed record JournalEntry(
    string Feature, int DocumentId, string DocumentCode, DateOnly Date, string Narration, IReadOnlyList<JournalEntryLine> Lines);

internal sealed record JournalEntryLine(string Key, decimal Amount, int? SupplierId = null)
{
    public static JournalEntryLine Debit(string key, decimal amount, int? supplierId = null) => new(key, amount, supplierId);

    public static JournalEntryLine Credit(string key, decimal amount, int? supplierId = null) => new(key, -amount, supplierId);
}

internal static class AccountPosting
{
    public const string CodePrefix = "JV-";

    public static async Task<(Journal? Journal, ProblemHttpResult? Problem)> PostAsync(
        ErpDbContext db, ICurrentContext current, JournalEntry entry, CancellationToken cancellationToken)
    {
        var lines = entry.Lines
            .GroupBy(l => (l.Key, l.SupplierId))
            .Select(g => new JournalEntryLine(g.Key.Key, g.Sum(l => l.Amount), g.Key.SupplierId))
            .Where(l => l.Amount != 0)
            .ToList();
        if (lines.Count == 0) return (null, null);
        if (lines.Sum(l => l.Amount) != 0)
        {
            throw new InvalidOperationException($"The journal of {entry.DocumentCode} does not balance.");
        }

        var keys = lines.Select(l => l.Key).Distinct().ToList();
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
            TotalAmount = lines.Where(l => l.Amount > 0).Sum(l => l.Amount),
            Status = ApprovalStatus.Approved,
            ReviewedAt = DateTime.UtcNow,
            ReviewedById = current.UserId,
        };
        journal.Lines.AddRange(lines
            .OrderByDescending(l => l.Amount > 0)
            .Select((l, index) => new JournalLine
            {
                Sequence = index + 1,
                ChartOfAccountId = accounts[l.Key],
                SupplierId = l.SupplierId,
                Amount = l.Amount,
            }));
        db.Journals.Add(journal);
        return (journal, null);
    }
}
