using Erp.Core.Domain.Approvals;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Approvals;

public sealed record ReviewRequest(string? Comment);

public sealed record ApprovalStepView(
    int Level,
    string Name,
    string? ApproverName,
    string Status,
    string? ActedByName,
    DateTimeOffset? ActedAt,
    string? Comment,
    bool IsCurrent);

public sealed record ApprovalProgress(IReadOnlyList<ApprovalStepView> Steps, bool CanAct);

internal sealed class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}

internal static class ApprovalFlow
{
    public const string DefaultStepName = "Approver";

    public static string StatusKey(ApprovalStatus status) => status.ToString().ToLowerInvariant();

    public static string StepKey(StepStatus status) => status.ToString().ToLowerInvariant();

    public static ProblemHttpResult NotPending(string action) =>
        TypedResults.Problem(detail: $"Only a pending record can be {action}.", statusCode: StatusCodes.Status409Conflict);

    public static ValidationProblem? MissingReason(ReviewRequest request) =>
        string.IsNullOrWhiteSpace(request.Comment)
            ? TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["comment"] = ["Write the reason."] })
            : null;

    public static async Task<List<ApprovalLevel>> LevelsAsync(
        ErpDbContext db, Guid accountId, Guid? businessUnitId, string feature, CancellationToken cancellationToken)
    {
        var levels = await db.ApprovalLevels.AcrossTenants().AsNoTracking()
            .Where(l => l.AccountId == accountId && l.Feature == feature && (l.BusinessUnitId == null || l.BusinessUnitId == businessUnitId))
            .OrderBy(l => l.Level)
            .ToListAsync(cancellationToken);
        var own = levels.Where(l => l.BusinessUnitId == businessUnitId && businessUnitId != null).ToList();
        return own.Count > 0 ? own : [.. levels.Where(l => l.BusinessUnitId == null)];
    }

    public static async Task StartAsync(
        ErpDbContext db, ICurrentContext current, string feature, int documentId, CancellationToken cancellationToken)
    {
        db.ApprovalSteps.RemoveRange(await db.ApprovalSteps
            .Where(s => s.Feature == feature && s.DocumentId == documentId)
            .ToListAsync(cancellationToken));
        db.ApprovalSteps.AddRange(await BuildAsync(db, current, feature, documentId, cancellationToken));
    }

    public static async Task RemoveAsync(ErpDbContext db, string feature, int documentId, CancellationToken cancellationToken) =>
        db.ApprovalSteps.RemoveRange(await db.ApprovalSteps
            .Where(s => s.Feature == feature && s.DocumentId == documentId)
            .ToListAsync(cancellationToken));

    public static async Task<ProblemHttpResult?> ActAsync(
        ErpDbContext db, ICurrentContext current, string feature, IApprovalDocument document, bool approve, string? comment,
        CancellationToken cancellationToken)
    {
        if (document.Status != ApprovalStatus.Pending) return NotPending("reviewed");

        var steps = await db.ApprovalSteps
            .Where(s => s.Feature == feature && s.DocumentId == document.Id)
            .OrderBy(s => s.Level)
            .ToListAsync(cancellationToken);
        if (steps.Count == 0)
        {
            steps = await BuildAsync(db, current, feature, document.Id, cancellationToken);
            db.ApprovalSteps.AddRange(steps);
        }

        var step = steps.First(s => s.Status == StepStatus.Pending);
        if (!await CanActAsync(db, current, step, cancellationToken))
        {
            return TypedResults.Problem(
                title: "Not your approval step",
                detail: $"This record is waiting for {step.Name}.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        step.Status = approve ? StepStatus.Approved : StepStatus.Rejected;
        step.ActedById = current.UserId;
        step.ActedAt = DateTimeOffset.UtcNow;
        step.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        if (!approve || steps.All(s => s.Status == StepStatus.Approved))
        {
            document.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            document.ReviewedAt = DateTime.UtcNow;
            document.ReviewedById = current.UserId;
            document.ReviewComment = step.Comment;
        }

        return null;
    }

    public static async Task<ProblemHttpResult?> CancelAsync(
        ErpDbContext db, ICurrentContext current, string feature, IApprovalDocument document, string? comment,
        Func<Task<ProblemHttpResult?>> undo, CancellationToken cancellationToken)
    {
        if (document.Status != ApprovalStatus.Approved)
        {
            return TypedResults.Problem(detail: "Only an approved record can be cancelled.", statusCode: StatusCodes.Status409Conflict);
        }

        if (await undo() is { } problem) return problem;

        var level = await db.ApprovalSteps
            .Where(s => s.Feature == feature && s.DocumentId == document.Id)
            .MaxAsync(s => (int?)s.Level, cancellationToken) ?? 0;
        db.ApprovalSteps.Add(new ApprovalStep
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Feature = feature,
            DocumentId = document.Id,
            Level = level + 1,
            Name = nameof(StepStatus.Cancelled),
            Status = StepStatus.Cancelled,
            ActedById = current.UserId,
            ActedAt = DateTimeOffset.UtcNow,
            Comment = comment?.Trim(),
        });
        document.Status = ApprovalStatus.Cancelled;
        return null;
    }

    public static ProblemHttpResult InUse(string detail) =>
        TypedResults.Problem(title: "Record in use", detail: detail, statusCode: StatusCodes.Status409Conflict);

    public static async Task<ApprovalProgress> ProgressAsync(
        ErpDbContext db, ICurrentContext current, string feature, IApprovalDocument document, CancellationToken cancellationToken)
    {
        var steps = await db.ApprovalSteps.AsNoTracking()
            .Where(s => s.Feature == feature && s.DocumentId == document.Id)
            .OrderBy(s => s.Level)
            .ToListAsync(cancellationToken);
        if (steps.Count == 0 && document.Status == ApprovalStatus.Pending)
        {
            steps = await BuildAsync(db, current, feature, document.Id, cancellationToken);
        }

        var pending = document.Status == ApprovalStatus.Pending
            ? steps.FirstOrDefault(s => s.Status == StepStatus.Pending)
            : null;
        var roleIds = steps.Select(s => s.RoleId).OfType<Guid>().ToList();
        var userIds = steps.SelectMany(s => new[] { s.UserId, s.ActedById }).OfType<Guid>().ToList();
        var roles = await db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
        var users = await db.ErpUsers.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var views = steps.Select(s => new ApprovalStepView(
            s.Level,
            s.Name,
            s.UserId is { } user ? users.GetValueOrDefault(user) : s.RoleId is { } role ? roles.GetValueOrDefault(role) : null,
            StepKey(s.Status),
            s.ActedById is { } acted ? users.GetValueOrDefault(acted) : null,
            s.ActedAt,
            s.Comment,
            s == pending)).ToList();

        return new ApprovalProgress(views, pending is not null && await CanActAsync(db, current, pending, cancellationToken));
    }

    public static async Task<bool> CanActAsync(ErpDbContext db, ICurrentContext current, ApprovalStep step, CancellationToken cancellationToken)
    {
        if (current.IsAccountAdmin) return true;
        if (step.UserId is { } userId) return userId == current.UserId;
        if (step.RoleId is not { } roleId) return true;
        return await db.UserBusinessUnits.AnyAsync(
            u => u.UserId == current.UserId && u.BusinessUnitId == current.BusinessUnitId && u.RoleId == roleId && u.IsActive,
            cancellationToken);
    }

    private static async Task<List<ApprovalStep>> BuildAsync(
        ErpDbContext db, ICurrentContext current, string feature, int documentId, CancellationToken cancellationToken)
    {
        var levels = await LevelsAsync(db, current.AccountId!.Value, current.BusinessUnitId, feature, cancellationToken);
        ApprovalStep Step(int level, string name, Guid? roleId, Guid? userId) => new()
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Feature = feature,
            DocumentId = documentId,
            Level = level,
            Name = name,
            RoleId = roleId,
            UserId = userId,
        };

        return levels.Count == 0
            ? [Step(1, DefaultStepName, null, null)]
            : [.. levels.Select((l, index) => Step(index + 1, l.Name, l.RoleId, l.UserId))];
    }
}
