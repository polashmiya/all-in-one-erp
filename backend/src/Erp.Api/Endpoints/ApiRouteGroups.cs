using Erp.Api.Auth;
using Erp.Api.Hosting;

namespace Erp.Api.Endpoints;

/// <summary>
/// The two API surfaces. Every endpoint in a group requires that surface's token unless it opts out
/// with AllowAnonymous (login). ERP endpoints also get the business-unit header filter.
/// </summary>
public static class ApiRouteGroups
{
    public const string PanelPrefix = "/api/panel";
    public const string ErpPrefix = "/api/erp";

    public static RouteGroupBuilder MapPanelApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PanelPrefix)
            .RequireAuthorization(AuthSurfaces.Panel)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.AddEndpointFilter<PasswordChangeFilter>();
        return group;
    }

    public static RouteGroupBuilder MapErpApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ErpPrefix)
            .RequireAuthorization(AuthSurfaces.Erp)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.AddEndpointFilter<PasswordChangeFilter>();
        group.AddEndpointFilter<BusinessUnitHeaderFilter>();
        return group;
    }
}
