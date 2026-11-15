using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Tenancy;

/// <summary>The roles every account starts with. Administrator always means every permission, so it stores none.</summary>
public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Viewer = "Viewer";

    public static bool IsAdministrator(Role role) => IsAdministrator(role.IsSystem, role.Name);

    public static bool IsAdministrator(bool isSystem, string name) => isSystem && name == Administrator;

    public static Role CreateAdministrator(Guid accountId, PermissionCatalog catalog) =>
        Create(accountId, Administrator, "Full access to every feature", []);

    public static Role CreateViewer(Guid accountId, PermissionCatalog catalog) =>
        Create(accountId, Viewer, "Read-only access with export", ViewerPermissions(catalog));

    public static IReadOnlyList<string> ViewerPermissions(PermissionCatalog catalog) =>
        catalog.All.Where(p => p.Code.Action is "view" or "export").Select(p => p.Code.Value).ToList();

    /// <summary>Gives every account's Viewer role the view and export permissions of features added since.</summary>
    public static async Task SyncViewersAsync(ErpDbContext db, PermissionCatalog catalog, CancellationToken cancellationToken)
    {
        var wanted = ViewerPermissions(catalog);
        var viewers = await db.Roles.AcrossTenants()
            .Include(r => r.Permissions)
            .Where(r => r.IsSystem && r.Name == Viewer)
            .ToListAsync(cancellationToken);

        foreach (var role in viewers)
        {
            foreach (var code in wanted.Where(code => role.Permissions.All(p => p.PermissionCode != code)))
            {
                role.Permissions.Add(new RolePermission { PermissionCode = code });
            }
        }
    }

    private static Role Create(Guid accountId, string name, string description, IEnumerable<string> permissions)
    {
        var role = new Role { AccountId = accountId, Name = name, Description = description, IsSystem = true };
        foreach (var code in permissions) role.Permissions.Add(new RolePermission { PermissionCode = code });
        return role;
    }
}
