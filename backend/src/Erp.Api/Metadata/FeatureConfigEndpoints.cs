using Erp.Core.Authorization;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Metadata;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Metadata;

public static class FeatureConfigEndpoints
{
    public static IEndpointRouteBuilder MapFeatureConfigEndpoints(this IEndpointRouteBuilder erp)
    {
        erp.MapGet("/features/{code}/config", Get)
            .WithName("GetFeatureConfig")
            .WithTags("FeatureConfig")
            .WithSummary("The feature's tabs, sections, fields, list columns and filters as configured for the account")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return erp;
    }

    private static async Task<Results<Ok<FeatureConfig>, NotFound, ProblemHttpResult>> Get(
        string code,
        ICurrentContext current,
        IPermissionService permissions,
        FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var permission = $"{code.ToLowerInvariant()}.{FeatureActions.View}";
        var granted = await permissions.GetCurrentAsync(cancellationToken);
        if (current.AccountId is not { } accountId || !granted.Has(permission))
        {
            return TypedResults.Problem(
                title: "Permission denied",
                detail: $"You need the '{permission}' permission in this business unit.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await configs.GetAsync(accountId, code, cancellationToken) is { } feature
            ? TypedResults.Ok(feature.Config)
            : TypedResults.NotFound();
    }
}
