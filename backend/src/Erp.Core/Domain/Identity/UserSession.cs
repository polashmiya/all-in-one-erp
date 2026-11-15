namespace Erp.Core.Domain.Identity;

public class UserSession : BaseEntity
{
    public required string Surface { get; set; }

    public Guid UserId { get; set; }

    public Guid FamilyId { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RotatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

public class LoginEvent : BaseEntity
{
    public required string Surface { get; set; }

    public Guid? UserId { get; set; }

    public required string Login { get; set; }

    public required string Result { get; set; }

    public bool Succeeded { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

public static class LoginResults
{
    public const string SignedIn = "signed-in";
    public const string WrongPassword = "wrong-password";
    public const string Locked = "locked";
    public const string Inactive = "inactive";
    public const string UnknownUser = "unknown-user";
    public const string NoBusinessUnit = "no-business-unit";
    public const string PasswordChanged = "password-changed";
    public const string PasswordReset = "password-reset";
}

public class PasswordResetCode : BaseEntity
{
    public required string Surface { get; set; }

    public Guid UserId { get; set; }

    public required string CodeHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset? UsedAt { get; set; }
}
