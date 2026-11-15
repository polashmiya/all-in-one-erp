using Erp.Api.Auth;
using Erp.Api.Users;
using FluentValidation;

namespace Erp.Api.Panel.ErpUsers;

public sealed record ErpUserUnitDto(Guid BusinessUnitId, string BusinessUnitName, string? RoleName, bool IsActive);

public sealed record ErpUserListItem(
    Guid Id,
    string FullName,
    string Username,
    string Email,
    string? Phone,
    Guid AccountId,
    string AccountName,
    bool IsAccountAdmin,
    bool IsActive,
    bool IsLocked,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<ErpUserUnitDto> Units);

public sealed record ErpUserCounts(int All, int Active, int Inactive);

public sealed record ErpUserListResponse(IReadOnlyList<ErpUserListItem> Items, int Total, ErpUserCounts Counts);

public sealed record ErpUserSessionDto(Guid Id, string? UserAgent, string? IpAddress, DateTimeOffset SignedInAt, DateTimeOffset LastUsedAt);

public sealed record LoginEventDto(DateTimeOffset At, string Result, bool Succeeded, string? IpAddress, string? UserAgent);

public sealed record AccessChangeDto(DateTimeOffset At, string? Before, string? After, string? ChangedBy);

public sealed record ErpUserDetail(
    ErpUserListItem User,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LockedUntil,
    IReadOnlyList<ErpUserSessionDto> Sessions,
    IReadOnlyList<LoginEventDto> Logins,
    IReadOnlyList<AccessChangeDto> History);

public sealed record UpdateErpUserRequest(string FullName, string Username, string Email, string? Phone) : IErpUserProfile;

public sealed record ErpUserStatusRequest(bool IsActive);

public sealed record ResetErpUserPasswordRequest(string? Password);

public sealed record ResetErpUserPasswordResult(string? GeneratedPassword);

public sealed record AddAccountAdminRequest(string FullName, string Username, string Email, string? Phone, string Password)
    : IErpUserProfile;

public sealed record UnitFeaturesDto(IReadOnlyList<string> DisabledFeatureCodes);

internal sealed class UpdateErpUserRequestValidator : AbstractValidator<UpdateErpUserRequest>
{
    public UpdateErpUserRequestValidator() => Include(new ErpUserProfileValidator());
}

internal sealed class AddAccountAdminRequestValidator : AbstractValidator<AddAccountAdminRequest>
{
    public AddAccountAdminRequestValidator()
    {
        Include(new ErpUserProfileValidator());
        RuleFor(x => x.Password).NotEmpty().StrongPassword();
    }
}

internal sealed class ResetErpUserPasswordRequestValidator : AbstractValidator<ResetErpUserPasswordRequest>
{
    public ResetErpUserPasswordRequestValidator()
    {
        RuleFor(x => x.Password!).StrongPassword().When(x => !string.IsNullOrEmpty(x.Password));
    }
}

internal sealed class UnitFeaturesDtoValidator : AbstractValidator<UnitFeaturesDto>
{
    public UnitFeaturesDtoValidator()
    {
        RuleFor(x => x.DisabledFeatureCodes).NotNull();
    }
}
