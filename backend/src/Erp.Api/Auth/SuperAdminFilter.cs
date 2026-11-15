using Erp.Core.Domain.Identity;
using Erp.Core.Security;

namespace Erp.Api.Auth;

public static class SuperAdminEndpointExtensions
{
    public static TBuilder RequireSuperAdmin<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
            context.HttpContext.User.FindFirst(ErpClaimTypes.Role)?.Value == nameof(PanelRole.SuperAdmin)
                ? await next(context)
                : TypedResults.Problem(
                    title: "SuperAdmin only",
                    detail: "Only a Panel SuperAdmin can change ERP users.",
                    statusCode: StatusCodes.Status403Forbidden));
}
