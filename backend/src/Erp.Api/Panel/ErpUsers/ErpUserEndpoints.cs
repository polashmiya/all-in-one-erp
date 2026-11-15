using Erp.Api.Auth;
using Erp.Api.Panel.Configuration;
using Erp.Api.Users;
using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.ErpUsers;

public static class ErpUserEndpoints
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int RecentLogins = 50;

    public static IEndpointRouteBuilder MapPanelErpUserEndpoints(this IEndpointRouteBuilder panel)
    {
        var users = panel.MapGroup("/erp-users").WithTags("PanelErpUsers");

        users.MapGet("", List)
            .WithName("ListErpUsers")
            .WithSummary("Every ERP user of every account, with their units and roles");

        users.MapGet("/{id:guid}", Get)
            .WithName("GetErpUser")
            .WithSummary("An ERP user with units and roles, signed-in devices, recent sign-ins and access history")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPut("/{id:guid}", Update)
            .WithName("UpdateErpUser")
            .WithSummary("Change name, username, email and phone")
            .RequireSuperAdmin()
            .Validate<UpdateErpUserRequest>();

        users.MapPut("/{id:guid}/status", SetStatus)
            .WithName("SetErpUserStatus")
            .WithSummary("Turn the user's sign-in on or off; turning it off ends every session")
            .RequireSuperAdmin();

        users.MapPost("/{id:guid}/reset-password", ResetPassword)
            .WithName("ResetErpUserPassword")
            .WithSummary("Set or generate a password the user must change at the next sign-in")
            .RequireSuperAdmin()
            .Validate<ResetErpUserPasswordRequest>();

        users.MapPost("/{id:guid}/unlock", Unlock)
            .WithName("UnlockErpUser")
            .WithSummary("Clear a lockout after too many wrong passwords")
            .RequireSuperAdmin();

        users.MapPost("/{id:guid}/sign-out", SignOut)
            .WithName("SignOutErpUser")
            .WithSummary("End every session of the user")
            .RequireSuperAdmin();

        panel.MapPost("/accounts/{id:guid}/admins", AddAdmin)
            .WithName("AddAccountAdmin")
            .WithTags("PanelErpUsers")
            .WithSummary("Add an account admin: full access in every business unit")
            .RequireSuperAdmin()
            .Validate<AddAccountAdminRequest>();

        return panel;
    }

    private static async Task<ErpUserListResponse> List(
        Guid? accountId,
        Guid? businessUnitId,
        string? status,
        string? type,
        string? search,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var users = db.ErpUsers.AcrossTenants().AsNoTracking();
        if (accountId is not null) users = users.Where(u => u.AccountId == accountId);
        if (businessUnitId is not null)
        {
            users = users.Where(u => u.IsAccountAdmin || u.BusinessUnits.Any(x => x.BusinessUnitId == businessUnitId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            users = users.Where(u =>
                u.FullName.Contains(term) || u.Username.Contains(term) || u.Email.Contains(term) ||
                (u.Phone != null && u.Phone.Contains(term)));
        }

        users = type switch
        {
            "admins" => users.Where(u => u.IsAccountAdmin),
            "users" => users.Where(u => !u.IsAccountAdmin),
            _ => users,
        };

        var counts = new ErpUserCounts(
            await users.CountAsync(cancellationToken),
            await users.CountAsync(u => u.IsActive, cancellationToken),
            await users.CountAsync(u => !u.IsActive, cancellationToken));

        users = status switch
        {
            "active" => users.Where(u => u.IsActive),
            "inactive" => users.Where(u => !u.IsActive),
            "locked" => users.Where(u => u.LockedUntil > now),
            _ => users,
        };

        var total = await users.CountAsync(cancellationToken);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var sorted = sort switch
        {
            "-fullName" => users.OrderByDescending(u => u.FullName),
            "lastLoginAt" => users.OrderBy(u => u.LastLoginAt),
            "-lastLoginAt" => users.OrderByDescending(u => u.LastLoginAt),
            _ => users.OrderBy(u => u.FullName),
        };

        var rows = await sorted.Skip((Math.Max(1, page ?? 1) - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new ErpUserListResponse(await ToItemsAsync(db, rows, now, cancellationToken), total, counts);
    }

    private static async Task<Results<Ok<ErpUserDetail>, NotFound>> Get(
        Guid id, ErpDbContext db, TimeProvider clock, CancellationToken cancellationToken)
    {
        var user = await db.ErpUsers.AcrossTenants().AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var now = clock.GetUtcNow();
        var item = (await ToItemsAsync(db, [user], now, cancellationToken)).Single();

        var families = await db.UserSessions.AsNoTracking()
            .Where(s => s.Surface == AuthSurfaces.Erp && s.UserId == id && s.RevokedAt == null && s.RotatedAt == null && s.ExpiresAt > now)
            .Select(s => s.FamilyId)
            .ToListAsync(cancellationToken);
        var sessions = (await db.UserSessions.AsNoTracking()
                .Where(s => families.Contains(s.FamilyId))
                .ToListAsync(cancellationToken))
            .GroupBy(s => s.FamilyId)
            .Select(g =>
            {
                var latest = g.MaxBy(s => s.CreatedAt)!;
                return new ErpUserSessionDto(g.Key, latest.UserAgent, latest.IpAddress, g.Min(s => s.CreatedAt), latest.CreatedAt);
            })
            .OrderByDescending(s => s.LastUsedAt)
            .ToList();

        var logins = await db.LoginEvents.AsNoTracking()
            .Where(e => e.Surface == AuthSurfaces.Erp && e.UserId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(RecentLogins)
            .Select(e => new LoginEventDto(e.CreatedAt, e.Result, e.Succeeded, e.IpAddress, e.UserAgent))
            .ToListAsync(cancellationToken);

        var key = id.ToString();
        var history = await db.ConfigurationChanges.AcrossTenants().AsNoTracking()
            .Where(c => c.AccountId == user.AccountId && c.Area == AccessAreas.User && c.Key == key)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new AccessChangeDto(
                c.CreatedAt,
                c.Before,
                c.After,
                db.PanelUsers.Where(p => p.Id == c.CreatedBy).Select(p => p.FullName).FirstOrDefault() ??
                db.ErpUsers.AcrossTenants().Where(e => e.Id == c.CreatedBy).Select(e => e.FullName).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new ErpUserDetail(
            item, user.MustChangePassword, user.CreatedAt, user.LockedUntil > now ? user.LockedUntil : null, sessions, logins, history));
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Update(
        Guid id, UpdateErpUserRequest request, ErpDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.ErpUsers.AcrossTenants().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var errors = await ErpUserProfiles.CheckUniqueAsync(db, request, id, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var before = Profile(user);
        ErpUserProfiles.Apply(user, request);
        ConfigurationHistory.Record(db, user.AccountId, null, null, AccessAreas.User, id.ToString(), before, Profile(user));
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> SetStatus(
        Guid id, ErpUserStatusRequest request, ErpDbContext db, SignInService signIn, CancellationToken cancellationToken)
    {
        var user = await db.ErpUsers.AcrossTenants().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return TypedResults.NotFound();
        if (user.IsActive == request.IsActive) return TypedResults.NoContent();

        if (!request.IsActive && user.IsAccountAdmin && !await db.ErpUsers.AcrossTenants().AnyAsync(
                u => u.AccountId == user.AccountId && u.IsAccountAdmin && u.IsActive && u.Id != id, cancellationToken))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["isActive"] = ["An account must keep at least one active admin."],
            });
        }

        user.IsActive = request.IsActive;
        if (!request.IsActive) await signIn.EndAllSessionsAsync(AuthSurfaces.Erp, user, cancellationToken);
        ConfigurationHistory.Record(db, user.AccountId, null, null, AccessAreas.User, id.ToString(),
            new { signIn = !request.IsActive }, new { signIn = request.IsActive });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ResetErpUserPasswordResult>, NotFound>> ResetPassword(
        Guid id, ResetErpUserPasswordRequest request, ErpDbContext db, SignInService signIn, CancellationToken cancellationToken)
    {
        var user = await db.ErpUsers.AcrossTenants().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var generated = string.IsNullOrEmpty(request.Password) ? PasswordGenerator.Generate() : null;
        await signIn.SetPasswordAsync(AuthSurfaces.Erp, user, generated ?? request.Password!, mustChange: true, cancellationToken);
        ConfigurationHistory.Record(db, user.AccountId, null, null, AccessAreas.User, id.ToString(), null, new { passwordReset = true });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new ResetErpUserPasswordResult(generated));
    }

    private static async Task<Results<NoContent, NotFound>> Unlock(Guid id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.ErpUsers.AcrossTenants().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        user.LockedUntil = null;
        user.FailedLoginCount = 0;
        ConfigurationHistory.Record(db, user.AccountId, null, null, AccessAreas.User, id.ToString(), null, new { unlocked = true });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> SignOut(
        Guid id, ErpDbContext db, SignInService signIn, CancellationToken cancellationToken)
    {
        var user = await db.ErpUsers.AcrossTenants().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        await signIn.EndAllSessionsAsync(AuthSurfaces.Erp, user, cancellationToken);
        ConfigurationHistory.Record(db, user.AccountId, null, null, AccessAreas.User, id.ToString(), null, new { signedOut = true });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ErpUserListItem>, NotFound, ValidationProblem>> AddAdmin(
        Guid id,
        AddAccountAdminRequest request,
        ErpDbContext db,
        IPasswordHasher hasher,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (!await db.Accounts.AcrossTenants().AnyAsync(a => a.Id == id, cancellationToken)) return TypedResults.NotFound();

        var errors = await ErpUserProfiles.CheckUniqueAsync(db, request, null, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var user = new ErpUser
        {
            AccountId = id,
            Username = "",
            Email = "",
            FullName = "",
            PasswordHash = hasher.Hash(request.Password),
            IsAccountAdmin = true,
            MustChangePassword = true,
        };
        ErpUserProfiles.Apply(user, request);
        db.ErpUsers.Add(user);
        ConfigurationHistory.Record(db, id, null, null, AccessAreas.User, user.Id.ToString(), null, Profile(user));
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok((await ToItemsAsync(db, [user], clock.GetUtcNow(), cancellationToken)).Single());
    }

    private static object Profile(ErpUser user) => new { user.FullName, user.Username, user.Email, user.Phone };

    private static async Task<List<ErpUserListItem>> ToItemsAsync(
        ErpDbContext db, IReadOnlyCollection<ErpUser> users, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var ids = users.Select(u => u.Id).ToList();
        var accountIds = users.Select(u => u.AccountId).Distinct().ToList();

        var accounts = await db.Accounts.AcrossTenants().AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var units = (await db.UserBusinessUnits.AcrossTenants().AsNoTracking()
                .Where(x => ids.Contains(x.UserId))
                .OrderBy(x => x.BusinessUnit!.Name)
                .Select(x => new { x.UserId, Unit = new ErpUserUnitDto(x.BusinessUnitId, x.BusinessUnit!.Name, x.Role!.Name, x.IsActive) })
                .ToListAsync(cancellationToken))
            .ToLookup(x => x.UserId, x => x.Unit);

        return users.Select(u => new ErpUserListItem(
                u.Id,
                u.FullName,
                u.Username,
                u.Email,
                u.Phone,
                u.AccountId,
                accounts.GetValueOrDefault(u.AccountId, ""),
                u.IsAccountAdmin,
                u.IsActive,
                u.LockedUntil > now,
                u.LastLoginAt,
                units[u.Id].ToList()))
            .ToList();
    }
}
