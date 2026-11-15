using Erp.Core.Domain.Tenancy;

namespace Erp.Core.Domain.Identity;

/// <summary>
/// A client user. Access to a business unit comes from a <see cref="UserBusinessUnit"/> row (which carries the role),
/// except for the account's primary admin, who can use every active unit with every permission.
/// </summary>
public class ErpUser : BaseEntity, IAccountOwned, ISignInUser
{
    public Guid AccountId { get; set; }

    public Account? Account { get; set; }

    /// <summary>Stored lower-cased; unique across all accounts so login needs only email + password.</summary>
    /// <summary>Stored lower-cased; unique across all accounts. Sign-in accepts the email or the username.</summary>
    public required string Username { get; set; }

    public required string Email { get; set; }

    public required string FullName { get; set; }

    public required string PasswordHash { get; set; }

    public string? Phone { get; set; }

    public int? EmployeeId { get; set; }

    public bool IsAccountAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Pre-selected in the business-unit picker on the next login.</summary>
    public Guid? LastBusinessUnitId { get; set; }

    public ICollection<UserBusinessUnit> BusinessUnits { get; } = [];
}
