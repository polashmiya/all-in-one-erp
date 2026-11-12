using System.Text.RegularExpressions;
using Erp.Core.Domain;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Inventory.ItemManagement;

internal static partial class MasterData
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 1000;
    public const int MaxBatch = 500;
    public const int MaxImageLength = 700_000;

    public static (Guid AccountId, Guid BusinessUnitId) Scope(this ICurrentContext current) =>
        current is { AccountId: { } account, BusinessUnitId: { } unit }
            ? (account, unit)
            : throw new InvalidOperationException("Item master data needs an account and a business unit.");

    public static async Task<(T Request, ValidationProblem? Rejected)> ConfiguredAsync<T>(
        FeatureConfigService configs, ICurrentContext current, string feature, T request, object? existing, CancellationToken ct)
    {
        var (accountId, businessUnitId) = current.Scope();
        var (configured, errors) = await configs.ApplyRulesAsync(accountId, businessUnitId, feature, request, existing, ct);
        return (configured, Invalid(errors));
    }

    public static string? SortKey(string? sort) => sort?.EndsWith("Id", StringComparison.Ordinal) == true ? sort[..^2] : sort;

    public static IQueryable<T> WithStatus<T>(this IQueryable<T> query, string? status) where T : MasterRecord =>
        status?.ToLowerInvariant() switch
        {
            "active" => query.Where(x => x.IsActive),
            "inactive" => query.Where(x => !x.IsActive),
            _ => query,
        };

    public static async Task<StatusCounts> CountByStatusAsync<T>(this IQueryable<T> query, CancellationToken ct) where T : MasterRecord
    {
        var all = await query.CountAsync(ct);
        var active = await query.CountAsync(x => x.IsActive, ct);
        return new StatusCounts(all, active, all - active);
    }

    public static int TotalFor(this StatusCounts counts, string? status) => status?.ToLowerInvariant() switch
    {
        "active" => counts.Active,
        "inactive" => counts.Inactive,
        _ => counts.All,
    };

    public static IQueryable<T> Page<T>(this IQueryable<T> query, int? page, int? pageSize)
    {
        var number = Math.Max(1, page ?? 1);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        return query.Skip((number - 1) * size).Take(size);
    }

    public static string? Search(string? search) => string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    public static string? CleanImage(string? image) => string.IsNullOrWhiteSpace(image) ? null : image.Trim();

    public static async Task<Results<Ok<AffectedRecords>, NotFound>> SetStatusAsync<T>(
        ErpDbContext db, RecordStatusRequest request, CancellationToken ct) where T : MasterRecord
    {
        var ids = request.Ids.Distinct().ToList();
        var records = await db.Set<T>().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        if (records.Count != ids.Count) return TypedResults.NotFound();

        foreach (var record in records) record.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new AffectedRecords(records.Count));
    }

    public static async Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> DeleteAsync<T>(
        ErpDbContext db,
        IEnumerable<int> ids,
        Func<List<int>, CancellationToken, Task<string?>> findUsage,
        CancellationToken ct) where T : MasterRecord
    {
        var distinct = ids.Distinct().ToList();
        var records = await db.Set<T>().Where(x => distinct.Contains(x.Id)).ToListAsync(ct);
        if (records.Count != distinct.Count) return TypedResults.NotFound();

        if (await findUsage(distinct, ct) is { } usage) return InUse(usage);

        db.Set<T>().RemoveRange(records);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 547 })
        {
            return InUse("Other records still refer to it. Deactivate it instead.");
        }

        return TypedResults.Ok(new AffectedRecords(records.Count));
    }

    public static ProblemHttpResult InUse(string detail) =>
        TypedResults.Problem(title: "Record in use", detail: detail, statusCode: StatusCodes.Status409Conflict);

    public static ProblemHttpResult Duplicate() =>
        TypedResults.Problem(
            title: "Duplicate record",
            detail: "Another record with the same code or name was saved a moment ago.",
            statusCode: StatusCodes.Status409Conflict);

    public static bool IsUniqueViolation(DbUpdateException e) => e.InnerException is SqlException { Number: 2601 or 2627 };

    public static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    public static ValidationProblem? Invalid(Dictionary<string, string[]> errors) =>
        errors.Count > 0 ? TypedResults.ValidationProblem(errors) : null;

    public static string Plural(int count, string one, string many) => count == 1 ? $"1 {one}" : $"{count} {many}";

    public static IRuleBuilderOptions<T, string> RecordCode<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty().WithMessage("Enter a code.")
            .Must(v => v is null || v.Trim().Length <= maxLength).WithMessage($"Use at most {maxLength} characters.")
            .Must(v => v is null || CodePattern().IsMatch(v.Trim())).WithMessage("Use letters, digits, dot, dash or underscore.");

    public static IRuleBuilderOptions<T, string> RecordName<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty().WithMessage("Enter a name.")
            .Must(v => v is null || v.Trim().Length <= maxLength).WithMessage($"Use at most {maxLength} characters.");

    public static IRuleBuilderOptions<T, string?> ImageSource<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(BeImage).WithMessage("Use a PNG, JPEG, WebP or GIF image of up to 500 KB, or an http(s) link.");

    private static bool BeImage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value.Length > MaxImageLength) return false;
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return DataImagePattern().IsMatch(value);
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex CodePattern();

    [GeneratedRegex("^data:image/(png|jpeg|webp|gif);base64,[A-Za-z0-9+/]+={0,2}$")]
    private static partial Regex DataImagePattern();
}
