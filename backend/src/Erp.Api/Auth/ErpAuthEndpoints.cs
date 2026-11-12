using System.Security.Claims;
using Erp.Api.Hosting;
using Erp.Api.Panel.Accounts;
using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Auth;

public static class ErpAuthEndpoints
{
    public static IEndpointRouteBuilder MapErpAuthEndpoints(this IEndpointRouteBuilder erp)
    {
        var auth = erp.MapGroup("/auth").WithTags("ErpAuth");

        auth.MapPost("/login", Login)
            .WithName("ErpLogin")
            .WithSummary("Sign in to the ERP; returns the business units the user can work in")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitSetup.LoginPolicy)
            .Validate<LoginRequest>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        auth.MapGet("/me", Me)
            .WithName("GetErpSession")
            .WithSummary("The signed-in user and their business units")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/select-unit", SelectUnit)
            .WithName("SelectBusinessUnit")
            .WithSummary("Switch to a business unit; returns the user's role and permissions there")
            .Validate<SelectUnitRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return erp;
    }

    private static async Task<Results<Ok<ErpLoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request,
        ErpDbContext db,
        IPasswordHasher hasher,
        TokenService tokens,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        // No tenant is known before login, so this lookup deliberately bypasses the tenant filter.
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.ErpUsers.AcrossTenants()
            .Include(u => u.Account)
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            hasher.SimulateVerify();
            return AuthProblems.InvalidCredentials();
        }

        if (!hasher.Verify(request.Password, user.PasswordHash) || !user.IsActive || user.Account?.IsActive != true)
        {
            return AuthProblems.InvalidCredentials();
        }

        var units = await GetBusinessUnitsAsync(db, user, cancellationToken);
        if (units.Count == 0)
        {
            return TypedResults.Problem(
                title: "No business unit assigned",
                detail: "Your user has no active business unit. Contact your administrator.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        user.LastLoginAt = clock.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        var token = tokens.Issue(AuthSurfaces.Erp,
        [
            new Claim(ErpClaimTypes.Subject, user.Id.ToString()),
            new Claim(ErpClaimTypes.Email, user.Email),
            new Claim(ErpClaimTypes.Name, user.FullName),
            new Claim(ErpClaimTypes.AccountId, user.AccountId.ToString()),
            new Claim(ErpClaimTypes.PrimaryAdmin, user.IsPrimaryAdmin ? "true" : "false"),
        ]);

        return TypedResults.Ok(new ErpLoginResponse(
            token.AccessToken, token.ExpiresAt, ToDto(user), units, LastUsed(user, units), AccountTheme.Parse(user.Account?.Theme)));
    }

    private static async Task<Results<Ok<ErpSessionResponse>, UnauthorizedHttpResult>> Me(
        ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        // Tenant filter applies: the user must belong to the account in the token.
        var user = await db.ErpUsers.Include(u => u.Account)
            .SingleOrDefaultAsync(u => u.Id == current.UserId && u.IsActive, cancellationToken);
        if (user is null) return TypedResults.Unauthorized();

        var units = await GetBusinessUnitsAsync(db, user, cancellationToken);
        return TypedResults.Ok(new ErpSessionResponse(ToDto(user), units, LastUsed(user, units)));
    }

    private static async Task<Results<Ok<SelectUnitResponse>, ProblemHttpResult, UnauthorizedHttpResult>> SelectUnit(
        SelectUnitRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IBusinessUnitAccess access,
        IPermissionService permissions,
        CancellationToken cancellationToken)
    {
        if (current.UserId is not { } userId || current.AccountId is not { } accountId) return TypedResults.Unauthorized();

        if (!await access.CanAccessAsync(userId, accountId, request.BusinessUnitId, current.IsPrimaryAdmin, cancellationToken))
        {
            return TypedResults.Problem(
                title: "Business unit not accessible",
                detail: "You do not have access to this business unit, or it is inactive.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var user = await db.ErpUsers.Include(u => u.Account).SingleAsync(u => u.Id == userId, cancellationToken);
        user.LastBusinessUnitId = request.BusinessUnitId;
        await db.SaveChangesAsync(cancellationToken);

        var unit = (await GetBusinessUnitsAsync(db, user, cancellationToken)).Single(u => u.Id == request.BusinessUnitId);
        var granted = await permissions.GetAsync(userId, accountId, request.BusinessUnitId, current.IsPrimaryAdmin, cancellationToken);

        return TypedResults.Ok(new SelectUnitResponse(unit, granted.Codes.Order(StringComparer.Ordinal).ToList()));
    }

    /// <summary>
    /// Active units the user can work in. A primary admin sees every active unit of the account; their role name
    /// comes from an explicit assignment when one exists.
    /// </summary>
    private static async Task<List<BusinessUnitAccessDto>> GetBusinessUnitsAsync(
        ErpDbContext db, ErpUser user, CancellationToken cancellationToken)
    {
        var assignments = await db.UserBusinessUnits.AcrossTenants()
            .Where(x => x.UserId == user.Id && x.AccountId == user.AccountId)
            .Select(x => new { x.BusinessUnitId, RoleName = x.Role!.Name })
            .ToDictionaryAsync(x => x.BusinessUnitId, x => x.RoleName, cancellationToken);

        var units = await db.BusinessUnits.AcrossTenants()
            .Where(b => b.AccountId == user.AccountId && b.IsActive)
            .Where(b => user.IsPrimaryAdmin || assignments.Keys.Contains(b.Id))
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Code, b.Name })
            .ToListAsync(cancellationToken);

        return units
            .Select(b => new BusinessUnitAccessDto(b.Id, b.Code, b.Name, assignments.GetValueOrDefault(b.Id) ?? "Primary admin"))
            .ToList();
    }

    private static Guid? LastUsed(ErpUser user, List<BusinessUnitAccessDto> units) =>
        units.Any(u => u.Id == user.LastBusinessUnitId) ? user.LastBusinessUnitId : null;

    private static ErpUserDto ToDto(ErpUser user) =>
        new(user.Id, user.Email, user.FullName, user.AccountId, user.Account?.Name ?? "", user.IsPrimaryAdmin);
}
