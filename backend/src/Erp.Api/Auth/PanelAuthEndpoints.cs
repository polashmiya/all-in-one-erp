using Erp.Api.Hosting;
using Erp.Core.Domain.Identity;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Auth;

public static class PanelAuthEndpoints
{
    public static IEndpointRouteBuilder MapPanelAuthEndpoints(this IEndpointRouteBuilder panel)
    {
        var auth = panel.MapGroup("/auth").WithTags("PanelAuth");

        auth.MapPost("/login", Login)
            .WithName("PanelLogin")
            .WithSummary("Sign in to the Panel")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitSetup.LoginPolicy)
            .Validate<LoginRequest>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapGet("/me", Me)
            .WithName("GetPanelSession")
            .WithSummary("The signed-in Panel user")
            .AllowBeforePasswordChange()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapSessionEndpoints(AuthSurfaces.Panel, "Panel");

        return panel;
    }

    private static async Task<Results<Ok<PanelLoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request,
        ErpDbContext db,
        SignInService signIn,
        CancellationToken cancellationToken)
    {
        var login = UserNames.Normalize(request.Login);
        var found = await signIn.FindByLoginAsync(AuthSurfaces.Panel, login, cancellationToken);

        if (!await signIn.TryPasswordAsync(AuthSurfaces.Panel, login, found, request.Password, cancellationToken) ||
            found is not PanelUser user)
        {
            return AuthProblems.InvalidCredentials();
        }

        var tokens = signIn.SignIn(AuthSurfaces.Panel, user, login);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new PanelLoginResponse(
            tokens.AccessToken, tokens.ExpiresAt, tokens.RefreshToken, tokens.RefreshExpiresAt, tokens.MustChangePassword,
            ToDto(user)));
    }

    private static async Task<Results<Ok<PanelUserDto>, UnauthorizedHttpResult>> Me(
        ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.PanelUsers.SingleOrDefaultAsync(u => u.Id == current.UserId && u.IsActive, cancellationToken);
        return user is null ? TypedResults.Unauthorized() : TypedResults.Ok(ToDto(user));
    }

    private static PanelUserDto ToDto(PanelUser user) => new(user.Id, user.Email, user.FullName, user.Role.ToString());
}

internal static class AuthProblems
{
    /// <summary>Same response for unknown email, wrong password, locked and disabled users, so accounts cannot be enumerated.</summary>
    public static ProblemHttpResult InvalidCredentials() =>
        TypedResults.Problem(title: "Invalid email or password", statusCode: StatusCodes.Status401Unauthorized);

    public static ProblemHttpResult SessionEnded() =>
        TypedResults.Problem(title: "Session ended", detail: "Sign in again.", statusCode: StatusCodes.Status401Unauthorized);
}
