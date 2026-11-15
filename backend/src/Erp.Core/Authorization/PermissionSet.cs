namespace Erp.Core.Authorization;

/// <summary>The permissions a user holds in one business unit (already limited to the account's features).</summary>
public sealed class PermissionSet
{
    private readonly HashSet<string> _codes;

    private PermissionSet(IEnumerable<string> codes)
    {
        _codes = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }

    public static PermissionSet Empty { get; } = new([]);

    public static PermissionSet Of(IEnumerable<string> codes) => new(codes);

    public IReadOnlyCollection<string> Codes => _codes;

    public bool Has(string code) => _codes.Contains(code);
}

/// <summary>A permission a module exposes, e.g. system-setup.item-profile.export.</summary>
public sealed record PermissionDefinition(PermissionCode Code, string Description);

/// <summary>Modules register one of these to contribute permissions to the catalog.</summary>
public interface IPermissionProvider
{
    IEnumerable<PermissionDefinition> GetPermissions();
}

public interface IPermissionService
{
    /// <summary>Permissions of the current user in the current business unit (empty when no unit is selected).</summary>
    Task<PermissionSet> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Effective permissions: everything for the account's primary admin, the role's grants otherwise,
    /// in both cases limited to features the account has been given.
    /// </summary>
    Task<PermissionSet> GetAsync(
        Guid userId, Guid accountId, Guid businessUnitId, bool isAccountAdmin, CancellationToken cancellationToken = default);
}

public interface IBusinessUnitAccess
{
    /// <summary>True when the ERP user may work in the business unit (assigned, or primary admin of its account; unit active).</summary>
    Task<bool> CanAccessAsync(Guid userId, Guid accountId, Guid businessUnitId, bool isAccountAdmin, CancellationToken cancellationToken = default);
}
