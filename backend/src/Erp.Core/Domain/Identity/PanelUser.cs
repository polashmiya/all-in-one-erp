namespace Erp.Core.Domain.Identity;

public enum PanelRole
{
    SuperAdmin,
    Implementer,
}

/// <summary>Internal team member who configures accounts in the Panel. Not tied to any account.</summary>
public class PanelUser : BaseEntity, ISignInUser
{
    /// <summary>Stored lower-cased; unique.</summary>
    public required string Email { get; set; }

    public required string FullName { get; set; }

    public required string PasswordHash { get; set; }

    public PanelRole Role { get; set; } = PanelRole.Implementer;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public DateTimeOffset? LastLoginAt { get; set; }
}
