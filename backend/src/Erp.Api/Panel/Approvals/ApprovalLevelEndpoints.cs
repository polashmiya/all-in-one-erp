using Erp.Api.Panel.Configuration;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Metadata;
using Erp.Core.Modules;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Approvals;

public sealed record ApprovalFeatureOption(string Code, LocalizedLabel Module, LocalizedLabel Label);

public sealed record ApproverOption(Guid Id, string Name, string? Email, bool IsActive);

public sealed record ApproverOptions(IReadOnlyList<ApproverOption> Roles, IReadOnlyList<ApproverOption> Users);

public sealed record ApprovalLevelDto(string Name, Guid? RoleId, Guid? UserId);

public sealed record ApprovalLevelSet(IReadOnlyList<ApprovalLevelDto> Levels, bool Inherited);

public sealed record SaveApprovalLevelsRequest(IReadOnlyList<ApprovalLevelDto> Levels);

internal sealed class SaveApprovalLevelsRequestValidator : AbstractValidator<SaveApprovalLevelsRequest>
{
    public const int MaxLevels = 10;

    public SaveApprovalLevelsRequestValidator()
    {
        RuleFor(x => x.Levels.Count).LessThanOrEqualTo(MaxLevels).OverridePropertyName("levels");
        RuleForEach(x => x.Levels).ChildRules(level =>
        {
            level.RuleFor(l => l.Name).NotEmpty().WithMessage("Enter a name for the level.").MaximumLength(100);
            level.RuleFor(l => l).Must(l => (l.RoleId is null) != (l.UserId is null))
                .WithMessage("Choose a role or a user who approves this level.")
                .OverridePropertyName("approver");
        });
    }
}

public static class ApprovalLevelEndpoints
{
    public static IEndpointRouteBuilder MapApprovalLevelEndpoints(this IEndpointRouteBuilder panel)
    {
        var account = panel.MapGroup("/accounts/{id:guid}").WithTags("PanelApprovals");

        account.MapGet("/approval-features", Features)
            .WithName("ListApprovalFeatures")
            .WithSummary("The account's screens that have an approval step")
            .ProducesProblem(StatusCodes.Status404NotFound);

        account.MapGet("/approvers", Approvers)
            .WithName("ListApprovers")
            .WithSummary("Roles and users of the account that can be set as approvers")
            .ProducesProblem(StatusCodes.Status404NotFound);

        account.MapGet("/approval-levels", Get)
            .WithName("GetApprovalLevels")
            .WithSummary("Approval levels of a screen for the account, or for one business unit (falls back to the account)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        account.MapPut("/approval-levels", Save)
            .WithName("SaveApprovalLevels")
            .WithSummary("Replace the approval levels of a screen; an empty list for a business unit uses the account's levels again")
            .Validate<SaveApprovalLevelsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return panel;
    }

    private static async Task<Results<Ok<List<ApprovalFeatureOption>>, NotFound>> Features(
        Guid id, ErpDbContext db, ErpMenuCatalog catalog, CancellationToken cancellationToken)
    {
        if (!await db.Accounts.AnyAsync(a => a.Id == id, cancellationToken)) return TypedResults.NotFound();
        var granted = await GrantedAsync(db, id, catalog, cancellationToken);
        return TypedResults.Ok(catalog.Features
            .Where(f => granted.Contains(f.Code))
            .Select(f => new ApprovalFeatureOption(f.Code, Label(f.Module.Name), Label(f.Feature.Name)))
            .ToList());
    }

    private static async Task<Results<Ok<ApproverOptions>, NotFound>> Approvers(Guid id, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Accounts.AnyAsync(a => a.Id == id, cancellationToken)) return TypedResults.NotFound();

        var roles = await db.Roles.AcrossTenants().AsNoTracking()
            .Where(r => r.AccountId == id)
            .OrderBy(r => r.Name)
            .Select(r => new ApproverOption(r.Id, r.Name, null, r.IsActive))
            .ToListAsync(cancellationToken);
        var users = await db.ErpUsers.AcrossTenants().AsNoTracking()
            .Where(u => u.AccountId == id)
            .OrderBy(u => u.FullName)
            .Select(u => new ApproverOption(u.Id, u.FullName, u.Email, u.IsActive))
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(new ApproverOptions(roles, users));
    }

    private static async Task<Results<Ok<ApprovalLevelSet>, NotFound>> Get(
        Guid id, string feature, Guid? businessUnitId, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (!await ScopeExistsAsync(db, id, businessUnitId, cancellationToken)) return TypedResults.NotFound();

        var own = await LevelsAsync(db, id, feature, businessUnitId, cancellationToken);
        if (own.Count > 0 || businessUnitId is null) return TypedResults.Ok(new ApprovalLevelSet(Dtos(own), false));
        return TypedResults.Ok(new ApprovalLevelSet(Dtos(await LevelsAsync(db, id, feature, null, cancellationToken)), true));
    }

    private static async Task<Results<Ok<ApprovalLevelSet>, NotFound, ValidationProblem>> Save(
        Guid id, string feature, Guid? businessUnitId, SaveApprovalLevelsRequest request, ErpDbContext db, ErpMenuCatalog catalog,
        CancellationToken cancellationToken)
    {
        if (!await ScopeExistsAsync(db, id, businessUnitId, cancellationToken)) return TypedResults.NotFound();

        var errors = new Dictionary<string, string[]>();
        if (!(await GrantedAsync(db, id, catalog, cancellationToken)).Contains(feature))
        {
            errors["feature"] = ["Choose a screen of this account that has approval."];
        }

        var roleIds = request.Levels.Select(l => l.RoleId).OfType<Guid>().ToList();
        var userIds = request.Levels.Select(l => l.UserId).OfType<Guid>().ToList();
        var roles = await db.Roles.AcrossTenants().CountAsync(r => r.AccountId == id && roleIds.Contains(r.Id), cancellationToken);
        var users = await db.ErpUsers.AcrossTenants().CountAsync(u => u.AccountId == id && userIds.Contains(u.Id), cancellationToken);
        if (roles != roleIds.Distinct().Count() || users != userIds.Distinct().Count())
        {
            errors["levels"] = ["Every approver must be a role or user of this account."];
        }

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var current = await db.ApprovalLevels.AcrossTenants()
            .Where(l => l.AccountId == id && l.Feature == feature && l.BusinessUnitId == businessUnitId)
            .OrderBy(l => l.Level)
            .ToListAsync(cancellationToken);
        var before = Dtos(current);
        db.ApprovalLevels.RemoveRange(current);
        db.ApprovalLevels.AddRange(request.Levels.Select((l, index) => new ApprovalLevel
        {
            AccountId = id,
            BusinessUnitId = businessUnitId,
            Feature = feature,
            Level = index + 1,
            Name = l.Name.Trim(),
            RoleId = l.RoleId,
            UserId = l.UserId,
        }));
        var after = request.Levels.Select(l => l with { Name = l.Name.Trim() }).ToList();
        ConfigurationHistory.Record(db, id, businessUnitId, feature, ConfigurationAreas.Approval, "levels",
            before.Count > 0 ? before : null, after.Count > 0 ? after : null);
        await db.SaveChangesAsync(cancellationToken);

        return (await Get(id, feature, businessUnitId, db, cancellationToken)).Result is Ok<ApprovalLevelSet> ok
            ? ok
            : TypedResults.NotFound();
    }

    private static async Task<HashSet<string>> GrantedAsync(ErpDbContext db, Guid id, ErpMenuCatalog catalog, CancellationToken cancellationToken)
    {
        var approvable = catalog.Features.Where(f => f.Actions.Contains("approve")).Select(f => f.Code).ToList();
        return (await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == id && approvable.Contains(af.Feature!.Code))
            .Select(af => af.Feature!.Code)
            .ToListAsync(cancellationToken)).ToHashSet();
    }

    private static async Task<bool> ScopeExistsAsync(ErpDbContext db, Guid id, Guid? businessUnitId, CancellationToken cancellationToken) =>
        businessUnitId is { } unitId
            ? await db.BusinessUnits.AcrossTenants().AnyAsync(u => u.Id == unitId && u.AccountId == id, cancellationToken)
            : await db.Accounts.AnyAsync(a => a.Id == id, cancellationToken);

    private static Task<List<ApprovalLevel>> LevelsAsync(
        ErpDbContext db, Guid id, string feature, Guid? businessUnitId, CancellationToken cancellationToken) =>
        db.ApprovalLevels.AcrossTenants().AsNoTracking()
            .Where(l => l.AccountId == id && l.Feature == feature && l.BusinessUnitId == businessUnitId)
            .OrderBy(l => l.Level)
            .ToListAsync(cancellationToken);

    private static List<ApprovalLevelDto> Dtos(IEnumerable<ApprovalLevel> levels) =>
        [.. levels.Select(l => new ApprovalLevelDto(l.Name, l.RoleId, l.UserId))];

    private static LocalizedLabel Label(MenuText text) => new(text.En, text.Bn);
}
