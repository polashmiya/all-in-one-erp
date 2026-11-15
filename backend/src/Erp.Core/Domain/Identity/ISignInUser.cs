namespace Erp.Core.Domain.Identity;

public interface ISignInUser
{
    Guid Id { get; }

    string Email { get; }

    string FullName { get; }

    string PasswordHash { get; set; }

    bool IsActive { get; }

    bool MustChangePassword { get; set; }

    int FailedLoginCount { get; set; }

    DateTimeOffset? LockedUntil { get; set; }

    Guid SecurityStamp { get; set; }

    DateTimeOffset? LastLoginAt { get; set; }
}
