using FluentValidation;

namespace Erp.Api.Auth;

public sealed class AuthOptions
{
    public const string Section = "Auth";

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);

    public TimeSpan RefreshReuseGrace { get; set; } = TimeSpan.FromMinutes(1);

    public int LockoutAttempts { get; set; } = 3;

    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan SessionCheckCache { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan ResetCodeLifetime { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan ResetCodeResendAfter { get; set; } = TimeSpan.FromMinutes(1);

    public int ResetCodeAttempts { get; set; } = 5;
}

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.Length(MinLength, MaxLength)
            .Matches("[A-Z]").WithMessage("Include an upper-case letter.")
            .Matches("[a-z]").WithMessage("Include a lower-case letter.")
            .Matches("[0-9]").WithMessage("Include a digit.");
}

public static class UserNames
{
    public const int MinLength = 3;
    public const int MaxLength = 50;
    public const string Pattern = "^[a-z0-9._-]+$";

    public static string Normalize(string value) => value.Trim().ToLowerInvariant();

    public static bool IsEmail(string login) => login.Contains('@');

    public static IRuleBuilderOptions<T, string> ValidUsername<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Length(MinLength, MaxLength)
            .Must(value => System.Text.RegularExpressions.Regex.IsMatch(Normalize(value), Pattern))
            .WithMessage("Use letters, digits, dot, dash or underscore only.");
}
