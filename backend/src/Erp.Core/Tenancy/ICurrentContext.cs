namespace Erp.Core.Tenancy;

/// <summary>Who is calling and in which tenant scope. Resolved per request from the JWT and X-Business-Unit header.</summary>
public interface ICurrentContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    /// <summary>Set for ERP users only; Panel users work across accounts.</summary>
    Guid? AccountId { get; }

    /// <summary>The validated business unit for this request, or null when none was selected.</summary>
    Guid? BusinessUnitId { get; }

    bool IsAccountAdmin { get; }
}
