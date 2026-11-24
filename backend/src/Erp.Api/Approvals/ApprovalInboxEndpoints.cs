using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Approvals;

public sealed record ApprovalInboxRow(
    string Feature,
    int Id,
    string Code,
    DateOnly Date,
    string? Party,
    decimal Amount,
    string Status,
    int Level,
    int Levels,
    string StepName,
    string? RequestedBy,
    DateTime? SubmittedAt,
    DateTimeOffset? ActedAt,
    string? Comment);

public sealed record ApprovalInboxCounts(int Waiting, int Approved, int Rejected);

public sealed record ApprovalInbox(IReadOnlyList<ApprovalInboxRow> Items, int Total, ApprovalInboxCounts Counts);

public static class ApprovalModule
{
    public static IServiceCollection AddApprovalModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, ApprovalMenu>();
        services.AddSingleton<IErpEndpoints, ApprovalInboxEndpoints>();
        return services;
    }
}

internal sealed class ApprovalInboxEndpoints : IErpEndpoints
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 1000;

    public void Map(IEndpointRouteBuilder erp)
    {
        erp.MapGet("/approvals", Inbox)
            .WithName("GetApprovalInbox")
            .WithTags("Approvals")
            .WithSummary("Records waiting for the current user's approval, or the ones they approved or rejected")
            .RequirePermission($"{ApprovalMenu.MyApprovals}.{FeatureActions.View}");
    }

    private static async Task<ApprovalInbox> Inbox(
        string? view,
        string? feature,
        string? search,
        int? page,
        int? pageSize,
        IEnumerable<IApprovalSource> sources,
        IPermissionService permissions,
        ICurrentContext current,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var granted = await permissions.GetCurrentAsync(cancellationToken);
        var mine = sources.Where(s => granted.Has($"{s.Feature}.approve") && (feature is null || s.Feature == feature)).ToList();
        var roles = await db.UserBusinessUnits.AsNoTracking()
            .Where(u => u.UserId == current.UserId && u.BusinessUnitId == current.BusinessUnitId && u.IsActive && u.RoleId != null)
            .Select(u => u.RoleId!.Value)
            .ToListAsync(cancellationToken);

        var waiting = new List<ApprovalInboxRow>();
        var acted = new List<ApprovalInboxRow>();
        foreach (var source in mine)
        {
            waiting.AddRange(await WaitingAsync(db, current, roles, source, cancellationToken));
            acted.AddRange(await ActedAsync(db, current, source, cancellationToken));
        }

        var counts = new ApprovalInboxCounts(
            waiting.Count,
            acted.Count(r => r.Status == ApprovalFlow.StepKey(StepStatus.Approved)),
            acted.Count(r => r.Status == ApprovalFlow.StepKey(StepStatus.Rejected)));
        IEnumerable<ApprovalInboxRow> rows = view switch
        {
            "approved" => acted.Where(r => r.Status == ApprovalFlow.StepKey(StepStatus.Approved)).OrderByDescending(r => r.ActedAt),
            "rejected" => acted.Where(r => r.Status == ApprovalFlow.StepKey(StepStatus.Rejected)).OrderByDescending(r => r.ActedAt),
            _ => waiting.OrderBy(r => r.SubmittedAt),
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            rows = rows.Where(r => r.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (r.Party?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var filtered = rows.ToList();
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var paged = filtered.Skip((Math.Max(1, page ?? 1) - 1) * size).Take(size).ToList();
        return new ApprovalInbox(paged, filtered.Count, counts);
    }

    private static async Task<List<ApprovalInboxRow>> WaitingAsync(
        ErpDbContext db, ICurrentContext current, List<Guid> roles, IApprovalSource source, CancellationToken cancellationToken)
    {
        var documents = await source.Documents(db, ApprovalStatus.Pending, null).ToListAsync(cancellationToken);
        var ids = documents.Select(d => d.Id).ToList();
        var steps = await db.ApprovalSteps.AsNoTracking()
            .Where(s => s.Feature == source.Feature && ids.Contains(s.DocumentId) && s.Status != StepStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var byDocument = steps.GroupBy(s => s.DocumentId).ToDictionary(g => g.Key, g => g.OrderBy(s => s.Level).ToList());

        var rows = new List<ApprovalInboxRow>();
        foreach (var document in documents)
        {
            var own = byDocument.GetValueOrDefault(document.Id) ?? [];
            var step = own.FirstOrDefault(s => s.Status == StepStatus.Pending);
            if (own.Count > 0 && step is null) continue;
            var canAct = current.IsAccountAdmin || step is null ||
                (step.UserId is { } user ? user == current.UserId : step.RoleId is not { } role || roles.Contains(role));
            if (!canAct) continue;

            rows.Add(Row(source.Feature, document, "pending", step?.Level ?? 1, Math.Max(1, own.Count),
                step?.Name ?? ApprovalFlow.DefaultStepName, null, null));
        }

        return rows;
    }

    private static async Task<List<ApprovalInboxRow>> ActedAsync(
        ErpDbContext db, ICurrentContext current, IApprovalSource source, CancellationToken cancellationToken)
    {
        var steps = await db.ApprovalSteps.AsNoTracking()
            .Where(s => s.Feature == source.Feature && s.ActedById == current.UserId &&
                (s.Status == StepStatus.Approved || s.Status == StepStatus.Rejected))
            .ToListAsync(cancellationToken);
        var ids = steps.Select(s => s.DocumentId).Distinct().ToList();
        var documents = await source.Documents(db, null, ids).ToDictionaryAsync(d => d.Id, cancellationToken);
        var levels = await db.ApprovalSteps.AsNoTracking()
            .Where(s => s.Feature == source.Feature && ids.Contains(s.DocumentId) && s.Status != StepStatus.Cancelled)
            .GroupBy(s => s.DocumentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        return [.. steps
            .Where(s => documents.ContainsKey(s.DocumentId))
            .Select(s => Row(source.Feature, documents[s.DocumentId], ApprovalFlow.StepKey(s.Status), s.Level,
                levels.GetValueOrDefault(s.DocumentId, 1), s.Name, s.ActedAt, s.Comment))];
    }

    private static ApprovalInboxRow Row(
        string feature, ApprovalDocumentInfo document, string status, int level, int levels, string stepName,
        DateTimeOffset? actedAt, string? comment) =>
        new(feature, document.Id, document.Code, document.Date, document.Party, document.Amount, status, level, levels, stepName,
            document.RequestedBy, document.CreatedAt, actedAt, comment);
}
