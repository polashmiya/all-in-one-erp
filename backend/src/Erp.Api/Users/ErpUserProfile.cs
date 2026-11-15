using Erp.Api.Auth;
using Erp.Core.Domain.Identity;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Users;

public interface IErpUserProfile
{
    string FullName { get; }

    string Username { get; }

    string Email { get; }

    string? Phone { get; }
}

public sealed class ErpUserProfileValidator : AbstractValidator<IErpUserProfile>
{
    public const int FullNameMax = 200;
    public const int EmailMax = 256;
    public const int PhoneMax = 32;

    public ErpUserProfileValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(FullNameMax);
        RuleFor(x => x.Username).ValidUsername();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(EmailMax);
        RuleFor(x => x.Phone).MaximumLength(PhoneMax);
    }
}

public static class ErpUserProfiles
{
    public static void Apply(ErpUser user, IErpUserProfile profile)
    {
        user.FullName = profile.FullName.Trim();
        user.Username = UserNames.Normalize(profile.Username);
        user.Email = UserNames.Normalize(profile.Email);
        user.Phone = string.IsNullOrWhiteSpace(profile.Phone) ? null : profile.Phone.Trim();
    }

    public static async Task<Dictionary<string, string[]>> CheckUniqueAsync(
        ErpDbContext db, IErpUserProfile profile, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        var username = UserNames.Normalize(profile.Username);
        var email = UserNames.Normalize(profile.Email);
        var others = db.ErpUsers.AcrossTenants().Where(u => u.Id != exceptUserId);
        var errors = new Dictionary<string, string[]>();

        if (await others.AnyAsync(u => u.Username == username, cancellationToken))
        {
            errors["username"] = ["A user with this username already exists."];
        }

        if (await others.AnyAsync(u => u.Email == email, cancellationToken))
        {
            errors["email"] = ["A user with this email already exists."];
        }

        return errors;
    }
}

public static class AccessAreas
{
    public const string User = "user";
    public const string Role = "role";
}
