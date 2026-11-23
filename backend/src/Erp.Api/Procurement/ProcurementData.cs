using Erp.Core.Domain.Procurement;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement;

public sealed record ApprovalCounts(int All, int Pending, int Approved, int Rejected, int Closed);

public sealed record ReviewRequest(string? Comment);

public sealed record ProcurementItemOption(
    int Id, string Code, string Name, int UomId, string UomName, decimal Rate, decimal VatPercentage, bool IsActive);

internal sealed class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}

internal static class ProcurementData
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 1000;
    public const int MaxLines = 500;
    public const string ChooseFromList = "Choose an active value from the list.";
    private const int CodeDigits = 5;
    private const int ItemOptionLimit = 20;

    public static string StatusKey(ApprovalStatus status) => status.ToString().ToLowerInvariant();

    public static async Task<(IQueryable<T> Documents, ApprovalCounts Counts)> ByStatusAsync<T>(
        IQueryable<T> documents, string? status, CancellationToken cancellationToken) where T : class, IApprovalDocument
    {
        var byStatus = await documents.GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);
        int Count(ApprovalStatus s) => byStatus.GetValueOrDefault(s);
        var counts = new ApprovalCounts(
            byStatus.Values.Sum(),
            Count(ApprovalStatus.Pending),
            Count(ApprovalStatus.Approved),
            Count(ApprovalStatus.Rejected),
            Count(ApprovalStatus.Closed));

        return Enum.TryParse<ApprovalStatus>(status, true, out var only)
            ? (documents.Where(d => d.Status == only), counts)
            : (documents, counts);
    }

    public static IQueryable<T> Page<T>(IQueryable<T> sorted, int? page, int? pageSize)
    {
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        return sorted.Skip((Math.Max(1, page ?? 1) - 1) * size).Take(size);
    }

    public static async Task<string?> CodeAsync<T>(
        IQueryable<T> documents, T document, string? sent, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
        where T : class, IApprovalDocument
    {
        var code = Trimmed(sent)?.ToUpperInvariant() ?? (document.Id > 0 ? document.Code : null);
        if (code is not null && await documents.AnyAsync(d => d.Id != document.Id && d.Code == code, cancellationToken))
        {
            errors["code"] = ["Another record already uses this number."];
        }

        return code;
    }

    public static async Task<string> NextCodeAsync<T>(IQueryable<T> documents, string prefix, CancellationToken cancellationToken)
        where T : class, IApprovalDocument
    {
        var codes = await documents.Where(d => d.Code.StartsWith(prefix)).Select(d => d.Code).ToListAsync(cancellationToken);
        var last = codes.Select(c => int.TryParse(c[prefix.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{(last + 1).ToString($"D{CodeDigits}")}";
    }

    public static ProblemHttpResult NotPending(string action) =>
        TypedResults.Problem(detail: $"Only a pending record can be {action}.", statusCode: StatusCodes.Status409Conflict);

    public static ValidationProblem? MissingReason(ReviewRequest request) =>
        Trimmed(request.Comment) is null
            ? TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["comment"] = ["Write the reason for rejecting."] })
            : null;

    public static ProblemHttpResult? Review(IApprovalDocument document, ApprovalStatus decision, string? comment, Guid? userId)
    {
        if (document.Status != ApprovalStatus.Pending) return NotPending("reviewed");
        document.Status = decision;
        document.ReviewedAt = DateTime.UtcNow;
        document.ReviewedById = userId;
        document.ReviewComment = Trimmed(comment);
        return null;
    }

    public static ProblemHttpResult? Close(IApprovalDocument document)
    {
        if (document.Status != ApprovalStatus.Approved)
        {
            return TypedResults.Problem(detail: "Only an approved record can be closed.", statusCode: StatusCodes.Status409Conflict);
        }

        document.Status = ApprovalStatus.Closed;
        return null;
    }

    public static ProblemHttpResult? Delete(IApprovalDocument document)
    {
        if (document.Status is not (ApprovalStatus.Pending or ApprovalStatus.Rejected))
        {
            return TypedResults.Problem(detail: "Only a pending or rejected record can be deleted.", statusCode: StatusCodes.Status409Conflict);
        }

        document.IsActive = false;
        return null;
    }

    public static Task<string?> ReviewerNameAsync(ErpDbContext db, Guid? userId, CancellationToken cancellationToken) =>
        userId is { } id
            ? db.ErpUsers.Where(u => u.Id == id).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken)
            : Task.FromResult<string?>(null);

    public static async Task CheckWarehouseAsync(
        ErpDbContext db, Guid warehouseId, Guid? current, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (warehouseId != current && !await db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.IsActive, cancellationToken))
        {
            errors["warehouseId"] = ["Choose an active warehouse of this business unit."];
        }
    }

    public static async Task<Dictionary<int, ProcurementItemOption>> CheckItemsAsync(
        ErpDbContext db, IReadOnlyList<int> itemIds, IEnumerable<int> saved, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var ids = itemIds.Distinct().ToList();
        var kept = saved.ToHashSet();
        var items = await ItemOptions(db.Items.AsNoTracking().Where(i => ids.Contains(i.Id))).ToListAsync(cancellationToken);

        var problems = new List<string>();
        if (ids.Count != itemIds.Count) problems.Add("Each item can be added only once.");
        if (items.Count != ids.Count || items.Any(i => !i.IsActive && !kept.Contains(i.Id)))
        {
            problems.Add("Choose active items of this business unit.");
        }

        if (problems.Count > 0) errors["lines"] = [.. problems];
        return items.ToDictionary(i => i.Id);
    }

    public static Task<List<ProcurementItemOption>> ItemsAsync(ErpDbContext db, string? search, CancellationToken cancellationToken)
    {
        var items = db.Items.AsNoTracking().Where(i => i.IsActive);
        if (Trimmed(search) is { } term)
        {
            items = items.Where(i => i.Code.Contains(term) || i.Name.Contains(term) || i.BarCode.Contains(term));
        }

        return ItemOptions(items.OrderBy(i => i.Name).Take(ItemOptionLimit)).ToListAsync(cancellationToken);
    }

    private static IQueryable<ProcurementItemOption> ItemOptions(IQueryable<Core.Domain.Inventory.Item> items) =>
        items.Select(i => new ProcurementItemOption(
            i.Id, i.Code, i.Name, i.UomId, i.Uom!.Name, i.LastCostPrice ?? i.AverageCostPrice ?? 0, i.PurchaseVatPercentage ?? 0,
            i.IsActive));

    public static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
