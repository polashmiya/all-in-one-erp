namespace Erp.Core.Security;

/// <summary>JWT claim names (raw, not mapped to WS-* URIs).</summary>
public static class ErpClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string AccountId = "account_id";
    public const string AccountAdmin = "account_admin";
    public const string SecurityStamp = "stamp";
    public const string SessionId = "sid";
    public const string MustChangePassword = "must_change_password";
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);

    /// <summary>Spends the same time as a real verification; used when the user does not exist.</summary>
    void SimulateVerify();
}
