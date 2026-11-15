namespace Erp.Core.Tenancy;

/// <summary>
/// Context for work outside a request (startup seeding, design-time tooling). Tenant filters match nothing,
/// so such code must opt out explicitly (AcrossTenants()).
/// </summary>
public sealed class SystemCurrentContext : ICurrentContext
{
    public static SystemCurrentContext Instance { get; } = new();

    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public Guid? AccountId => null;

    public Guid? BusinessUnitId => null;

    public bool IsAccountAdmin => false;
}
