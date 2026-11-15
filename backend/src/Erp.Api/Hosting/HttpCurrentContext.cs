using System.Security.Claims;
using Erp.Core.Security;
using Erp.Core.Tenancy;

namespace Erp.Api.Hosting;

/// <summary>
/// <see cref="ICurrentContext"/> for HTTP requests: identity from the validated JWT, business unit from the
/// X-Business-Unit header once <see cref="BusinessUnitHeaderFilter"/> has checked access.
/// </summary>
public sealed class HttpCurrentContext(IHttpContextAccessor accessor) : ICurrentContext
{
    private Guid? _businessUnitId;

    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId => ReadGuid(ErpClaimTypes.Subject);

    public Guid? AccountId => ReadGuid(ErpClaimTypes.AccountId);

    public Guid? BusinessUnitId => _businessUnitId;

    public bool IsAccountAdmin => IsAuthenticated && User?.FindFirst(ErpClaimTypes.AccountAdmin)?.Value == "true";

    public void SetBusinessUnit(Guid businessUnitId) => _businessUnitId = businessUnitId;

    private Guid? ReadGuid(string claimType) =>
        IsAuthenticated && Guid.TryParse(User?.FindFirst(claimType)?.Value, out var value) ? value : null;
}
