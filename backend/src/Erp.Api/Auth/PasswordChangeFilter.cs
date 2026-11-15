using Erp.Core.Security;

namespace Erp.Api.Auth;

public sealed record AllowBeforePasswordChangeMetadata;

public static class PasswordChangeEndpointExtensions
{
    public static TBuilder AllowBeforePasswordChange<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AllowBeforePasswordChangeMetadata());
}

public sealed class PasswordChangeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var mustChange = http.User.FindFirst(ErpClaimTypes.MustChangePassword)?.Value == "true";
        var allowed = http.GetEndpoint()?.Metadata.GetMetadata<AllowBeforePasswordChangeMetadata>() is not null;

        if (!mustChange || allowed) return await next(context);

        return TypedResults.Problem(
            title: "Password change required",
            detail: "Change your password before you continue.",
            statusCode: StatusCodes.Status403Forbidden);
    }
}
