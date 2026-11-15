using Erp.Core.Authorization;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Navigation;

public static class ErpNavigationEndpoints
{
    /// <summary>The signed-in user's ERP menu.</summary>
    public static IEndpointRouteBuilder MapErpNavigationEndpoints(this IEndpointRouteBuilder erp)
    {
        erp.MapGet("/navigation", GetNavigation)
            .WithName("GetErpNavigation")
            .WithTags("ErpNavigation")
            .WithSummary("Module → Sub-module → Feature menu: the account's features, limited to what the user may view");

        return erp;
    }

    /// <summary>The full catalog for the Panel (choosing which features an account gets).</summary>
    public static IEndpointRouteBuilder MapPanelErpMenuEndpoints(this IEndpointRouteBuilder panel)
    {
        panel.MapGet("/erp-menu", (ErpDbContext db, CancellationToken ct) => ErpMenuTree.BuildAsync(db, _ => true, ct))
            .WithName("GetErpMenuCatalog")
            .WithTags("PanelErpMenu")
            .WithSummary("Every active ERP module, sub-module and feature");

        return panel;
    }

    private static async Task<Results<Ok<IReadOnlyList<ErpMenuModule>>, UnauthorizedHttpResult>> GetNavigation(
        ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken cancellationToken)
    {
        if (current.AccountId is not { } accountId) return TypedResults.Unauthorized();

        Func<string, bool> include;
        if (current.IsAccountAdmin)
        {
            var features = current.BusinessUnitId is { } unitId
                ? await db.UnitFeatureCodesAsync(accountId, unitId, cancellationToken)
                : await db.AccountFeatureCodesAsync(accountId, cancellationToken);
            include = features.Contains;
        }
        else
        {
            var granted = await permissions.GetCurrentAsync(cancellationToken);
            include = code => granted.Has($"{code}.view");
        }

        var versions = await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == accountId)
            .Select(af => new { af.Feature!.Code, af.ConfigVersion })
            .ToDictionaryAsync(af => af.Code, af => af.ConfigVersion, cancellationToken);

        return TypedResults.Ok(await ErpMenuTree.BuildAsync(db, include, cancellationToken, versions));
    }
}
