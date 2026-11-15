using Erp.Api.Panel.Configuration;
using Erp.Api.Users;
using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.UserManagement.Roles;

internal static class RoleEndpoints
{
    private const string Feature = UserManagementMenu.UserRole;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/permission-tree", Tree)
            .WithName("GetPermissionTree")
            .WithSummary("Modules › sub-modules › screens with their actions, for the screens this account and unit have")
            .RequirePermission($"{Feature}.view");

        var roles = group.MapGroup("/roles");

        roles.MapGet("", List)
            .WithName("ListRoles")
            .WithSummary("The account's roles, with how many users have each one in the selected unit")
            .RequirePermission($"{Feature}.view");

        roles.MapGet("/options", Options)
            .WithName("GetRoleOptions")
            .WithSummary("Active roles, for assigning")
            .RequireAnyPermission($"{Feature}.view", $"{UserManagementMenu.User}.edit");

        roles.MapGet("/{id:guid}", Get)
            .WithName("GetRole")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        roles.MapPost("", Create)
            .WithName("CreateRole")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveRoleRequest>();

        roles.MapPut("/{id:guid}", Update)
            .WithName("UpdateRole")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveRoleRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        roles.MapPut("/{id:guid}/status", SetStatus)
            .WithName("SetRoleStatus")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound);

        roles.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteRole")
            .WithSummary("Delete a custom role that nobody has in any unit")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound);

        roles.MapPut("/{id:guid}/users", AssignUsers)
            .WithName("AssignRoleUsers")
            .WithSummary("Give this role to users of the selected unit; it replaces their role in this unit")
            .RequireAnyPermission($"{Feature}.edit", $"{UserManagementMenu.User}.edit")
            .Validate<RoleUsersRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        roles.MapDelete("/{id:guid}/users/{userId:guid}", RemoveUser)
            .WithName("RemoveRoleUser")
            .WithSummary("Take this role away from a user in the selected unit")
            .RequireAnyPermission($"{Feature}.edit", $"{UserManagementMenu.User}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<List<PermissionTreeModule>> Tree(
        ICurrentContext current, ErpDbContext db, ErpMenuCatalog catalog, CancellationToken cancellationToken)
    {
        var features = await db.UnitFeatureCodesAsync(current.AccountId!.Value, current.BusinessUnitId!.Value, cancellationToken);
        return catalog.Modules
            .Select(m => new PermissionTreeModule(m.Key, Label(m.Name), m.Icon, m.SubModules
                .Select(s => new PermissionTreeSubModule(s.Key, Label(s.Name), s.Icon, s.Features
                    .Select(f => new CatalogFeature(m, s, f))
                    .Where(f => features.Contains(f.Code))
                    .Select(f => new PermissionTreeFeature(f.Code, Label(f.Feature.Name), f.Actions))
                    .ToList()))
                .Where(s => s.Features.Count > 0)
                .ToList()))
            .Where(m => m.SubModules.Count > 0)
            .ToList();
    }

    private static async Task<RoleList> List(
        string? search,
        string? status,
        string? sort,
        int? page,
        int? pageSize,
        ICurrentContext current,
        ErpDbContext db,
        PermissionCatalog catalog,
        CancellationToken cancellationToken)
    {
        var unitId = current.BusinessUnitId!.Value;
        var roles = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            roles = roles.Where(r => r.Name.Contains(term) || (r.Description != null && r.Description.Contains(term)));
        }

        var counts = new RoleCounts(
            await roles.CountAsync(cancellationToken),
            await roles.CountAsync(r => r.IsActive, cancellationToken),
            await roles.CountAsync(r => !r.IsActive, cancellationToken));

        roles = status switch
        {
            "active" => roles.Where(r => r.IsActive),
            "inactive" => roles.Where(r => !r.IsActive),
            _ => roles,
        };

        var total = await roles.CountAsync(cancellationToken);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var sorted = sort == "-name" ? roles.OrderByDescending(r => r.Name) : roles.OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name);
        var rows = await sorted
            .Skip((Math.Max(1, page ?? 1) - 1) * size)
            .Take(size)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.IsSystem,
                r.IsActive,
                Permissions = r.Permissions.Count,
                Users = db.UserBusinessUnits.Count(x => x.RoleId == r.Id && x.BusinessUnitId == unitId),
            })
            .ToListAsync(cancellationToken);

        return new RoleList(
            rows.Select(r =>
            {
                var administrator = SystemRoles.IsAdministrator(r.IsSystem, r.Name);
                return new RoleRow(r.Id, r.Name, r.Description, r.IsSystem, administrator, r.IsActive,
                    administrator ? catalog.Codes.Count : r.Permissions, r.Users);
            }).ToList(),
            total,
            counts,
            catalog.Codes.Count);
    }

    private static async Task<List<RoleOption>> Options(ErpDbContext db, PermissionCatalog catalog, CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, r.Description, r.IsSystem, Permissions = r.Permissions.Count })
            .ToListAsync(cancellationToken);

        return roles.Select(r =>
        {
            var administrator = SystemRoles.IsAdministrator(r.IsSystem, r.Name);
            return new RoleOption(r.Id, r.Name, r.Description, administrator, administrator ? catalog.Codes.Count : r.Permissions);
        }).ToList();
    }

    private static async Task<Results<Ok<RoleDetail>, NotFound>> Get(
        Guid id, ICurrentContext current, ErpDbContext db, PermissionCatalog catalog, CancellationToken cancellationToken) =>
        await LoadAsync(id, current, db, catalog, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static async Task<RoleDetail?> LoadAsync(
        Guid id, ICurrentContext current, ErpDbContext db, PermissionCatalog catalog, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().Include(r => r.Permissions).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return null;

        var administrator = SystemRoles.IsAdministrator(role);
        var users = await db.UserBusinessUnits.AsNoTracking()
            .Where(x => x.RoleId == id && x.BusinessUnitId == current.BusinessUnitId)
            .OrderBy(x => x.User!.FullName)
            .Select(x => new RoleUserRow(x.UserId, x.User!.FullName, x.User.Username))
            .ToListAsync(cancellationToken);

        return new RoleDetail(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystem,
            administrator,
            role.IsActive,
            administrator ? [.. catalog.Codes] : role.Permissions.Select(p => p.PermissionCode).Order().ToList(),
            users);
    }

    private static async Task<Results<Ok<RoleDetail>, NotFound, ValidationProblem>> Create(
        SaveRoleRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        PermissionCatalog catalog,
        CancellationToken cancellationToken)
    {
        var errors = await CheckAsync(request, null, current, db, permissions, catalog, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var role = new Role { AccountId = current.AccountId!.Value, Name = request.Name.Trim() };
        Apply(role, request);
        db.Roles.Add(role);
        ConfigurationHistory.Record(db, role.AccountId, null, null, AccessAreas.Role, role.Id.ToString(), null, Snapshot(role));
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(role.Id, current, db, catalog, cancellationToken))!);
    }

    private static async Task<Results<Ok<RoleDetail>, NotFound, ValidationProblem>> Update(
        Guid id,
        SaveRoleRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        PermissionCatalog catalog,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles.Include(r => r.Permissions).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return TypedResults.NotFound();
        if (SystemRoles.IsAdministrator(role)) return Error("name", "The Administrator role always has every permission and cannot be changed.");

        var errors = await CheckAsync(request, role, current, db, permissions, catalog, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var before = Snapshot(role);
        if (!role.IsSystem) role.Name = request.Name.Trim();
        Apply(role, request);
        ConfigurationHistory.Record(db, role.AccountId, null, null, AccessAreas.Role, id.ToString(), before, Snapshot(role));
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(id, current, db, catalog, cancellationToken))!);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> SetStatus(
        Guid id, RoleStatusRequest request, ErpDbContext db, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return TypedResults.NotFound();
        if (SystemRoles.IsAdministrator(role)) return Error("isActive", "The Administrator role is always active.");

        var before = new { role.IsActive };
        role.IsActive = request.IsActive;
        ConfigurationHistory.Record(db, role.AccountId, null, null, AccessAreas.Role, id.ToString(), before, new { role.IsActive });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Delete(
        Guid id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return TypedResults.NotFound();
        if (role.IsSystem) return Error("id", "System roles cannot be deleted.");
        if (await db.UserBusinessUnits.AnyAsync(x => x.RoleId == id, cancellationToken))
        {
            return Error("id", "Users still have this role. Deactivate it instead.");
        }

        db.Roles.Remove(role);
        ConfigurationHistory.Record(db, role.AccountId, null, null, AccessAreas.Role, id.ToString(), new { role.Name }, null);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> AssignUsers(
        Guid id,
        RoleUsersRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        PermissionCatalog catalog,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles.Include(r => r.Permissions).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return TypedResults.NotFound();
        if (!role.IsActive) return Error("userIds", "Activate the role before giving it to users.");
        if (request.UserIds.Contains(current.UserId!.Value)) return Error("userIds", "You cannot change your own access.");
        if (!await CanGrantAsync(Codes(role, catalog), current, permissions, cancellationToken))
        {
            return Error("userIds", "You can only give a role whose permissions you have yourself in this unit.");
        }

        var rows = await db.UserBusinessUnits
            .Where(x => x.BusinessUnitId == current.BusinessUnitId && request.UserIds.Contains(x.UserId) && !x.User!.IsAccountAdmin)
            .ToListAsync(cancellationToken);
        if (rows.Count != request.UserIds.Distinct().Count()) return Error("userIds", "Every user must be in the selected unit.");

        foreach (var row in rows)
        {
            ConfigurationHistory.Record(db, row.AccountId, row.BusinessUnitId, null, AccessAreas.User, row.UserId.ToString(),
                new { roleId = row.RoleId }, new { roleId = id });
            row.RoleId = id;
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> RemoveUser(
        Guid id, Guid userId, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (userId == current.UserId) return Error("userId", "You cannot change your own access.");

        var row = await db.UserBusinessUnits
            .SingleOrDefaultAsync(x => x.BusinessUnitId == current.BusinessUnitId && x.UserId == userId && x.RoleId == id, cancellationToken);
        if (row is null) return TypedResults.NotFound();

        ConfigurationHistory.Record(db, row.AccountId, row.BusinessUnitId, null, AccessAreas.User, userId.ToString(),
            new { roleId = id }, new { roleId = (Guid?)null });
        row.RoleId = null;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Dictionary<string, string[]>> CheckAsync(
        SaveRoleRequest request,
        Role? role,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        PermissionCatalog catalog,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var name = request.Name.Trim();
        var roleId = role?.Id;
        if (role?.IsSystem != true && await db.Roles.AnyAsync(r => r.Name == name && r.Id != roleId, cancellationToken))
        {
            errors["name"] = ["A role with this name already exists."];
        }

        var features = await db.AccountFeatureCodesAsync(current.AccountId!.Value, cancellationToken);
        var unknown = request.Permissions.Where(code =>
            !catalog.Codes.Contains(code) || !PermissionCode.TryParse(code, out var parsed) || !features.Contains(parsed.FeatureCode)).ToList();
        if (unknown.Count > 0)
        {
            errors["permissions"] = [$"Unknown permission(s): {string.Join(", ", unknown)}."];
        }
        else if (!await CanGrantAsync(request.Permissions.Concat(role is null ? [] : Codes(role, catalog)), current, permissions, cancellationToken))
        {
            errors["permissions"] = ["You can only give permissions you have yourself in this unit."];
        }

        return errors;
    }

    internal static async Task<bool> CanGrantAsync(
        IEnumerable<string> codes, ICurrentContext current, IPermissionService permissions, CancellationToken cancellationToken)
    {
        if (current.IsAccountAdmin) return true;
        var mine = await permissions.GetCurrentAsync(cancellationToken);
        return codes.All(mine.Has);
    }

    internal static IEnumerable<string> Codes(Role role, PermissionCatalog catalog) =>
        SystemRoles.IsAdministrator(role) ? catalog.Codes : role.Permissions.Select(p => p.PermissionCode);

    private static void Apply(Role role, SaveRoleRequest request)
    {
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        role.IsActive = request.IsActive;
        var wanted = request.Permissions.Distinct().ToHashSet();
        foreach (var gone in role.Permissions.Where(p => !wanted.Contains(p.PermissionCode)).ToList()) role.Permissions.Remove(gone);
        foreach (var code in wanted.Where(code => role.Permissions.All(p => p.PermissionCode != code)))
        {
            role.Permissions.Add(new RolePermission { PermissionCode = code });
        }
    }

    private static object Snapshot(Role role) => new
    {
        role.Name,
        role.Description,
        role.IsActive,
        Permissions = role.Permissions.Count,
    };

    private static LocalizedLabel Label(MenuText text) => new(text.En, text.Bn);

    private static ValidationProblem Error(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
