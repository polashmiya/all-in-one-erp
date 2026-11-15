using Erp.Api.Panel.ErpUsers;
using Erp.Core.Domain.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Accounts;

public static class UnitFeatureEndpoints
{
    public static IEndpointRouteBuilder MapUnitFeatureEndpoints(this IEndpointRouteBuilder panel)
    {
        var unit = panel.MapGroup("/accounts/{id:guid}/business-units/{unitId:guid}/features").WithTags("PanelAccounts");

        unit.MapGet("", Get)
            .WithName("GetUnitFeatures")
            .WithSummary("The account's features that are turned off for this business unit")
            .ProducesProblem(StatusCodes.Status404NotFound);

        unit.MapPut("", Save)
            .WithName("SaveUnitFeatures")
            .WithSummary("Turn account features off (or back on) for one business unit; menus and permissions follow")
            .Validate<UnitFeaturesDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return panel;
    }

    private static async Task<Results<Ok<UnitFeaturesDto>, NotFound>> Get(
        Guid id, Guid unitId, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (!await UnitExistsAsync(db, id, unitId, cancellationToken)) return TypedResults.NotFound();

        var codes = await db.BusinessUnitFeatures.AcrossTenants()
            .Where(x => x.AccountId == id && x.BusinessUnitId == unitId)
            .OrderBy(x => x.Feature!.Code)
            .Select(x => x.Feature!.Code)
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(new UnitFeaturesDto(codes));
    }

    private static async Task<Results<Ok<UnitFeaturesDto>, NotFound>> Save(
        Guid id, Guid unitId, UnitFeaturesDto request, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (!await UnitExistsAsync(db, id, unitId, cancellationToken)) return TypedResults.NotFound();

        var wanted = request.DisabledFeatureCodes.Select(c => c.Trim().ToLowerInvariant()).ToHashSet();
        var accountFeatures = await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == id && wanted.Contains(af.Feature!.Code))
            .Select(af => new { af.FeatureId, af.Feature!.Code })
            .ToListAsync(cancellationToken);
        var featureIds = accountFeatures.Select(f => f.FeatureId).ToList();
        var current = await db.BusinessUnitFeatures.AcrossTenants()
            .Where(x => x.AccountId == id && x.BusinessUnitId == unitId)
            .ToListAsync(cancellationToken);

        db.BusinessUnitFeatures.RemoveRange(current.Where(x => !featureIds.Contains(x.FeatureId)));
        db.BusinessUnitFeatures.AddRange(featureIds
            .Where(featureId => current.All(x => x.FeatureId != featureId))
            .Select(featureId => new BusinessUnitFeature { AccountId = id, BusinessUnitId = unitId, FeatureId = featureId }));
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new UnitFeaturesDto(accountFeatures.Select(f => f.Code).Order().ToList()));
    }

    private static Task<bool> UnitExistsAsync(ErpDbContext db, Guid id, Guid unitId, CancellationToken cancellationToken) =>
        db.BusinessUnits.AcrossTenants().AnyAsync(b => b.Id == unitId && b.AccountId == id, cancellationToken);
}
