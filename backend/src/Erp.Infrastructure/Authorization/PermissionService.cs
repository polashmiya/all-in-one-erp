using Erp.Core.Authorization;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Authorization;

/// <summary>
/// Resolves permissions per (user, business unit), limited to the features the account has been given.
/// The current request's set is loaded once.
/// </summary>
public sealed class PermissionService(ErpDbContext db, ICurrentContext current, PermissionCatalog catalog) : IPermissionService
{
    private PermissionSet? _current;

    public async Task<PermissionSet> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (_current is not null) return _current;
        if (current.UserId is not { } userId || current.AccountId is not { } accountId || current.BusinessUnitId is not { } businessUnitId)
        {
            return PermissionSet.Empty;
        }

        return _current = await GetAsync(userId, accountId, businessUnitId, current.IsAccountAdmin, cancellationToken);
    }

    public async Task<PermissionSet> GetAsync(
        Guid userId, Guid accountId, Guid businessUnitId, bool isAccountAdmin, CancellationToken cancellationToken = default)
    {
        var features = await db.UnitFeatureCodesAsync(accountId, businessUnitId, cancellationToken);
        if (features.Count == 0) return PermissionSet.Empty;

        var role = isAccountAdmin
            ? null
            : await db.UserBusinessUnits
                .AcrossTenants()
                .Where(x => x.UserId == userId && x.BusinessUnitId == businessUnitId && x.AccountId == accountId &&
                            x.IsActive && x.Role!.IsActive)
                .Select(x => new
                {
                    x.Role!.IsSystem,
                    x.Role.Name,
                    Codes = x.Role.Permissions.Select(p => p.PermissionCode).ToList(),
                })
                .SingleOrDefaultAsync(cancellationToken);

        IEnumerable<string> granted = isAccountAdmin || (role is not null && SystemRoles.IsAdministrator(role.IsSystem, role.Name))
            ? catalog.Codes
            : role?.Codes ?? [];

        return PermissionSet.Of(granted.Where(code =>
            PermissionCode.TryParse(code, out var parsed) && features.Contains(parsed.FeatureCode)));
    }
}

/// <summary>Checks that an ERP user may work in a business unit. Explicit account checks; no reliance on filters.</summary>
public sealed class BusinessUnitAccess(ErpDbContext db) : IBusinessUnitAccess
{
    public async Task<bool> CanAccessAsync(
        Guid userId, Guid accountId, Guid businessUnitId, bool isAccountAdmin, CancellationToken cancellationToken = default)
    {
        var userActive = await db.ErpUsers.AcrossTenants()
            .AnyAsync(u => u.Id == userId && u.AccountId == accountId && u.IsActive, cancellationToken);
        if (!userActive) return false;

        var unitActive = await db.BusinessUnits.AcrossTenants()
            .AnyAsync(b => b.Id == businessUnitId && b.AccountId == accountId && b.IsActive && b.Account!.IsActive, cancellationToken);
        if (!unitActive) return false;

        return isAccountAdmin || await db.UserBusinessUnits.AcrossTenants()
            .AnyAsync(x => x.UserId == userId && x.BusinessUnitId == businessUnitId && x.AccountId == accountId && x.IsActive && x.RoleId != null, cancellationToken);
    }
}
