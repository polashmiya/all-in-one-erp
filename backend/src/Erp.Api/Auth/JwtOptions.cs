using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Erp.Api.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required]
    public string Issuer { get; set; } = "myErp";

    /// <summary>HMAC-SHA256 key. Development value lives in appsettings.Development.json; set Jwt__SigningKey elsewhere.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = "";

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

/// <summary>The two API surfaces each have their own audience, bearer scheme and policy.</summary>
public static class AuthSurfaces
{
    public const string Panel = "panel";
    public const string Erp = "erp";
}
