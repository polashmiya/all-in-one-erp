using Erp.Core.Authorization;

namespace Erp.Api.Hosting;

public sealed record IgnoreLostBusinessUnitMetadata;

public static class BusinessUnitHeaderExtensions
{
    public static TBuilder IgnoreLostBusinessUnit<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new IgnoreLostBusinessUnitMetadata());
}

/// <summary>
/// Group filter on /api/erp: validates X-Business-Unit against the caller's access and, if allowed, makes it the
/// request's business unit. A missing header leaves the unit unset (unit-scoped endpoints then reject the call).
/// </summary>
public sealed class BusinessUnitHeaderFilter : IEndpointFilter
{
    public const string HeaderName = "X-Business-Unit";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var header = http.Request.Headers[HeaderName].ToString();
        var current = http.RequestServices.GetRequiredService<HttpCurrentContext>();

        if (string.IsNullOrWhiteSpace(header) || !current.IsAuthenticated) return await next(context);

        if (!Guid.TryParse(header, out var businessUnitId))
        {
            return TypedResults.Problem(
                title: "Invalid business unit",
                detail: $"The {HeaderName} header must be a business unit id.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (current.UserId is not { } userId || current.AccountId is not { } accountId) return TypedResults.Unauthorized();

        var allowed = await http.RequestServices.GetRequiredService<IBusinessUnitAccess>()
            .CanAccessAsync(userId, accountId, businessUnitId, current.IsAccountAdmin, http.RequestAborted);

        if (!allowed)
        {
            if (http.GetEndpoint()?.Metadata.GetMetadata<IgnoreLostBusinessUnitMetadata>() is not null) return await next(context);

            return TypedResults.Problem(
                title: "Business unit not accessible",
                detail: "You do not have access to this business unit, or it is inactive.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        current.SetBusinessUnit(businessUnitId);
        return await next(context);
    }
}
