using Erp.Api.Auth;
using Erp.Api.Panel.Configuration;
using Erp.Api.UserManagement.Roles;
using Erp.Api.Users;
using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.UserManagement.Users;

internal static class UserEndpoints
{
    private const string Feature = UserManagementMenu.User;
    private const string AddToUnit = $"{Feature}.create";
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 1000;
    private const string NoRole = "none";

    public static void Map(IEndpointRouteBuilder group)
    {
        var users = group.MapGroup("/users");

        users.MapGet("", List)
            .WithName("ListUsers")
            .WithSummary("Users of the selected business unit, with their role there")
            .RequirePermission($"{Feature}.view");

        users.MapGet("/options", Options)
            .WithName("GetUserOptions")
            .WithSummary("Users of the selected unit, for assigning a role")
            .RequireAnyPermission($"{Feature}.view", $"{UserManagementMenu.UserRole}.edit");

        users.MapGet("/units", Units)
            .WithName("GetUserUnitChoices")
            .WithSummary("The account's active units, and whether you may add users to each")
            .RequireAnyPermission($"{Feature}.create", $"{Feature}.edit");

        users.MapGet("/{id:guid}", Get)
            .WithName("GetUser")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("", Create)
            .WithName("CreateUser")
            .WithSummary("Create a user and add them to the chosen units without a role")
            .RequirePermission($"{Feature}.create")
            .Validate<CreateUserRequest>();

        users.MapPut("/{id:guid}", Update)
            .WithName("UpdateUser")
            .WithSummary("Change the user's own details")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveUserRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/{id:guid}/units", AddUnits)
            .WithName("AddUserUnits")
            .WithSummary("Add the user to more units, without a role")
            .RequirePermission($"{Feature}.edit")
            .Validate<UserUnitsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPut("/{id:guid}/role", SetRole)
            .WithName("SetUserRole")
            .WithSummary("The user's role in the selected unit; null takes it away")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPut("/{id:guid}/status", SetStatus)
            .WithName("SetUserStatus")
            .WithSummary("Turn the user's access to the selected unit on or off")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/{id:guid}/sign-out", SignOut)
            .WithName("SignOutUser")
            .WithSummary("End every session of the user")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<UserList> List(
        string? search,
        string? status,
        string? role,
        string? signIn,
        string? sort,
        int? page,
        int? pageSize,
        ICurrentContext current,
        ErpDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var rows = InUnit(db, current.BusinessUnitId!.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            rows = rows.Where(r => r.User.FullName.Contains(term) || r.User.Username.Contains(term) || r.User.Email.Contains(term) ||
                (r.User.Phone != null && r.User.Phone.Contains(term)));
        }

        if (role == NoRole) rows = rows.Where(r => !r.User.IsAccountAdmin && r.Access!.RoleId == null);
        else if (Guid.TryParse(role, out var roleId)) rows = rows.Where(r => r.Access!.RoleId == roleId);
        rows = signIn switch
        {
            "locked" => rows.Where(r => r.User.LockedUntil > now),
            "never" => rows.Where(r => r.User.LastLoginAt == null),
            _ => rows,
        };

        var counts = new UserCounts(
            await rows.CountAsync(cancellationToken),
            await rows.CountAsync(r => r.User.IsActive && (r.User.IsAccountAdmin || r.Access!.IsActive), cancellationToken),
            await rows.CountAsync(r => !(r.User.IsActive && (r.User.IsAccountAdmin || r.Access!.IsActive)), cancellationToken));
        rows = status switch
        {
            "active" => rows.Where(r => r.User.IsActive && (r.User.IsAccountAdmin || r.Access!.IsActive)),
            "inactive" => rows.Where(r => !(r.User.IsActive && (r.User.IsAccountAdmin || r.Access!.IsActive))),
            _ => rows,
        };

        var total = await rows.CountAsync(cancellationToken);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var sorted = sort switch
        {
            "-fullName" => rows.OrderByDescending(r => r.User.FullName),
            "lastLoginAt" => rows.OrderBy(r => r.User.LastLoginAt),
            "-lastLoginAt" => rows.OrderByDescending(r => r.User.LastLoginAt),
            _ => rows.OrderBy(r => r.User.FullName),
        };

        var found = await sorted.Skip((Math.Max(1, page ?? 1) - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new UserList(await ToRowsAsync(db, found, now, cancellationToken), total, counts);
    }

    private static Task<List<UserOption>> Options(ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        InUnit(db, current.BusinessUnitId!.Value)
            .Where(r => r.User.IsActive && !r.User.IsAccountAdmin)
            .OrderBy(r => r.User.FullName)
            .Select(r => new UserOption(r.User.Id, r.User.FullName, r.User.Username, r.Access!.RoleId))
            .ToListAsync(cancellationToken);

    private static async Task<List<UnitChoice>> Units(
        ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken cancellationToken)
    {
        var units = await db.BusinessUnits.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync(cancellationToken);

        var choices = new List<UnitChoice>();
        foreach (var unit in units)
        {
            choices.Add(new UnitChoice(unit.Id, unit.Name, await CanAddToAsync(unit.Id, current, permissions, cancellationToken)));
        }

        return choices;
    }

    private static async Task<Results<Ok<UserDetail>, NotFound>> Get(
        Guid id, ICurrentContext current, ErpDbContext db, TimeProvider clock, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, current.BusinessUnitId!.Value, clock.GetUtcNow(), cancellationToken) is { } detail
            ? TypedResults.Ok(detail)
            : TypedResults.NotFound();

    private static async Task<Results<Ok<UserDetail>, ValidationProblem>> Create(
        CreateUserRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        IPasswordHasher hasher,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var errors = await ErpUserProfiles.CheckUniqueAsync(db, request, null, cancellationToken);
        await CheckEmployeeAsync(request.EmployeeId, null, db, errors, cancellationToken);
        await CheckUnitsAsync(request.BusinessUnitIds, current, db, permissions, errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var user = new ErpUser
        {
            AccountId = current.AccountId!.Value,
            Username = "",
            Email = "",
            FullName = "",
            PasswordHash = hasher.Hash(request.Password),
            MustChangePassword = true,
            EmployeeId = request.EmployeeId,
        };
        ErpUserProfiles.Apply(user, request);
        foreach (var unitId in request.BusinessUnitIds.Distinct())
        {
            user.BusinessUnits.Add(new UserBusinessUnit { AccountId = user.AccountId, BusinessUnitId = unitId });
        }

        db.ErpUsers.Add(user);
        ConfigurationHistory.Record(db, user.AccountId, current.BusinessUnitId, null, AccessAreas.User, user.Id.ToString(), null,
            new { user.FullName, user.Username, user.Email, user.Phone, units = request.BusinessUnitIds });
        await db.SaveChangesAsync(cancellationToken);

        var shownUnit = request.BusinessUnitIds.Contains(current.BusinessUnitId!.Value) ? current.BusinessUnitId.Value : request.BusinessUnitIds[0];
        return TypedResults.Ok((await LoadAsync(db, user.Id, shownUnit, clock.GetUtcNow(), cancellationToken))!);
    }

    private static async Task<Results<Ok<UserDetail>, NotFound, ValidationProblem>> Update(
        Guid id,
        SaveUserRequest request,
        ICurrentContext current,
        ErpDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(db, id, current, cancellationToken);
        if (user is null) return TypedResults.NotFound();
        if (user.IsAccountAdmin) return Error("fullName", AccountAdminReadOnly);

        var errors = await ErpUserProfiles.CheckUniqueAsync(db, request, id, cancellationToken);
        await CheckEmployeeAsync(request.EmployeeId, id, db, errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var before = new { user.FullName, user.Username, user.Email, user.Phone, user.EmployeeId };
        ErpUserProfiles.Apply(user, request);
        user.EmployeeId = request.EmployeeId;
        ConfigurationHistory.Record(db, user.AccountId, null, null, AccessAreas.User, id.ToString(), before,
            new { user.FullName, user.Username, user.Email, user.Phone, user.EmployeeId });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, current.BusinessUnitId!.Value, clock.GetUtcNow(), cancellationToken))!);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> AddUnits(
        Guid id,
        UserUnitsRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(db, id, current, cancellationToken, withUnits: true);
        if (user is null) return TypedResults.NotFound();
        if (user.IsAccountAdmin) return Error("businessUnitIds", AccountAdminReadOnly);
        if (id == current.UserId) return Error("businessUnitIds", OwnAccess);

        var errors = new Dictionary<string, string[]>();
        var added = request.BusinessUnitIds.Distinct().Where(u => user.BusinessUnits.All(x => x.BusinessUnitId != u)).ToList();
        await CheckUnitsAsync(added, current, db, permissions, errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        foreach (var unitId in added)
        {
            user.BusinessUnits.Add(new UserBusinessUnit { AccountId = user.AccountId, BusinessUnitId = unitId });
        }

        ConfigurationHistory.Record(db, user.AccountId, null, null, AccessAreas.User, id.ToString(), null, new { addedUnits = added });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> SetRole(
        Guid id,
        UserRoleRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        PermissionCatalog catalog,
        CancellationToken cancellationToken)
    {
        if (id == current.UserId) return Error("roleId", OwnAccess);
        var row = await AccessAsync(db, id, current, cancellationToken);
        if (row is null) return TypedResults.NotFound();
        if (row.User!.IsAccountAdmin) return Error("roleId", AccountAdminReadOnly);

        if (request.RoleId is { } roleId)
        {
            var role = await db.Roles.Include(r => r.Permissions).SingleOrDefaultAsync(r => r.Id == roleId && r.IsActive, cancellationToken);
            if (role is null) return Error("roleId", "Choose an active role.");
            if (!await RoleEndpoints.CanGrantAsync(RoleEndpoints.Codes(role, catalog), current, permissions, cancellationToken))
            {
                return Error("roleId", "You can only give a role whose permissions you have yourself in this unit.");
            }
        }

        ConfigurationHistory.Record(db, row.AccountId, row.BusinessUnitId, null, AccessAreas.User, id.ToString(),
            new { roleId = row.RoleId }, new { roleId = request.RoleId });
        row.RoleId = request.RoleId;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> SetStatus(
        Guid id, UserStatusRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (id == current.UserId) return Error("isActive", OwnAccess);
        var row = await AccessAsync(db, id, current, cancellationToken);
        if (row is null) return TypedResults.NotFound();
        if (row.User!.IsAccountAdmin) return Error("isActive", AccountAdminReadOnly);

        ConfigurationHistory.Record(db, row.AccountId, row.BusinessUnitId, null, AccessAreas.User, id.ToString(),
            new { row.IsActive }, new { request.IsActive });
        row.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> SignOut(
        Guid id, ICurrentContext current, ErpDbContext db, SignInService signIn, CancellationToken cancellationToken)
    {
        if (id == current.UserId) return Error("id", OwnAccess);
        var user = await FindAsync(db, id, current, cancellationToken);
        if (user is null) return TypedResults.NotFound();
        if (user.IsAccountAdmin) return Error("id", AccountAdminReadOnly);

        await signIn.EndAllSessionsAsync(AuthSurfaces.Erp, user, cancellationToken);
        ConfigurationHistory.Record(db, user.AccountId, current.BusinessUnitId, null, AccessAreas.User, id.ToString(), null, new { signedOut = true });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private const string OwnAccess = "You cannot change your own access.";
    private const string AccountAdminReadOnly = "Account admins are changed from the Panel only.";

    private sealed class UnitUser
    {
        public required ErpUser User { get; init; }

        public UserBusinessUnit? Access { get; init; }
    }

    private static IQueryable<UnitUser> InUnit(ErpDbContext db, Guid unitId) =>
        db.ErpUsers.AsNoTracking()
            .Select(u => new UnitUser { User = u, Access = u.BusinessUnits.FirstOrDefault(x => x.BusinessUnitId == unitId) })
            .Where(r => r.User.IsAccountAdmin || r.Access != null);

    private static Task<ErpUser?> FindAsync(
        ErpDbContext db, Guid id, ICurrentContext current, CancellationToken cancellationToken, bool withUnits = false)
    {
        IQueryable<ErpUser> users = withUnits ? db.ErpUsers.Include(u => u.BusinessUnits) : db.ErpUsers;
        return users.SingleOrDefaultAsync(
            u => u.Id == id && (u.IsAccountAdmin || u.BusinessUnits.Any(x => x.BusinessUnitId == current.BusinessUnitId)), cancellationToken);
    }

    private static Task<UserBusinessUnit?> AccessAsync(ErpDbContext db, Guid id, ICurrentContext current, CancellationToken cancellationToken) =>
        db.UserBusinessUnits.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.UserId == id && x.BusinessUnitId == current.BusinessUnitId, cancellationToken);

    private static async Task<bool> CanAddToAsync(
        Guid unitId, ICurrentContext current, IPermissionService permissions, CancellationToken cancellationToken) =>
        (await permissions.GetAsync(current.UserId!.Value, current.AccountId!.Value, unitId, current.IsAccountAdmin, cancellationToken))
        .Has(AddToUnit);

    private static async Task CheckUnitsAsync(
        IReadOnlyCollection<Guid> unitIds,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var active = await db.BusinessUnits.Where(b => unitIds.Contains(b.Id) && b.IsActive).Select(b => b.Id).ToListAsync(cancellationToken);
        foreach (var unitId in unitIds.Distinct())
        {
            if (!active.Contains(unitId) || !await CanAddToAsync(unitId, current, permissions, cancellationToken))
            {
                errors["businessUnitIds"] = ["You can only add users to active units where you may create users."];
                return;
            }
        }
    }

    private static async Task CheckEmployeeAsync(
        int? employeeId, Guid? userId, ErpDbContext db, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (employeeId is not { } id) return;
        if (!await db.Employees.AnyAsync(e => e.Id == id, cancellationToken))
        {
            errors["employeeId"] = ["Choose an employee from the list."];
        }
        else if (await db.ErpUsers.AnyAsync(u => u.EmployeeId == id && u.Id != userId, cancellationToken))
        {
            errors["employeeId"] = ["This employee already has a user."];
        }
    }

    private static async Task<UserDetail?> LoadAsync(
        ErpDbContext db, Guid id, Guid unitId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = await InUnit(db, unitId).SingleOrDefaultAsync(r => r.User.Id == id, cancellationToken);
        if (row is null) return null;

        var units = await db.UserBusinessUnits.AsNoTracking()
            .Where(x => x.UserId == id)
            .OrderBy(x => x.BusinessUnit!.Name)
            .Select(x => new UserUnit(x.BusinessUnitId, x.BusinessUnit!.Name, x.Role!.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return new UserDetail((await ToRowsAsync(db, [row], now, cancellationToken))[0], row.User.MustChangePassword, units);
    }

    private static async Task<List<UserRow>> ToRowsAsync(
        ErpDbContext db, IReadOnlyCollection<UnitUser> rows, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var employeeIds = rows.Where(r => r.User.EmployeeId != null).Select(r => r.User.EmployeeId!.Value).ToList();
        var employees = await db.Employees.AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
        var roleIds = rows.Where(r => r.Access?.RoleId != null).Select(r => r.Access!.RoleId!.Value).ToList();
        var roles = await db.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        return rows.Select(r => new UserRow(
                r.User.Id,
                r.User.FullName,
                r.User.Username,
                r.User.Email,
                r.User.Phone,
                r.User.EmployeeId,
                r.User.EmployeeId is { } e ? employees.GetValueOrDefault(e) : null,
                r.User.IsAccountAdmin,
                r.User.IsActive && (r.User.IsAccountAdmin || r.Access!.IsActive),
                r.Access?.RoleId,
                r.Access?.RoleId is { } roleId ? roles.GetValueOrDefault(roleId) : null,
                r.User.LockedUntil > now,
                r.User.LastLoginAt))
            .ToList();
    }

    private static ValidationProblem Error(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
