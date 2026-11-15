using Erp.Api.Auth;
using Erp.Api.Users;
using FluentValidation;

namespace Erp.Api.UserManagement.Users;

public sealed record UserCounts(int All, int Active, int Inactive);

public sealed record UserRow(
    Guid Id,
    string FullName,
    string Username,
    string Email,
    string? Phone,
    int? EmployeeId,
    string? EmployeeName,
    bool IsAccountAdmin,
    bool IsActive,
    Guid? RoleId,
    string? RoleName,
    bool IsLocked,
    DateTimeOffset? LastLoginAt);

public sealed record UserList(IReadOnlyList<UserRow> Items, int Total, UserCounts Counts);

public sealed record UserOption(Guid Id, string FullName, string Username, Guid? RoleId);

public sealed record UserUnit(Guid BusinessUnitId, string BusinessUnitName, string? RoleName, bool IsActive);

public sealed record UserDetail(UserRow User, bool MustChangePassword, IReadOnlyList<UserUnit> Units);

public sealed record UnitChoice(Guid Id, string Name, bool CanAdd);

public sealed record SaveUserRequest(string FullName, string Username, string Email, string? Phone, int? EmployeeId) : IErpUserProfile;

public sealed record CreateUserRequest(
    string FullName,
    string Username,
    string Email,
    string? Phone,
    int? EmployeeId,
    string Password,
    IReadOnlyList<Guid> BusinessUnitIds) : IErpUserProfile;

public sealed record UserUnitsRequest(IReadOnlyList<Guid> BusinessUnitIds);

public sealed record UserRoleRequest(Guid? RoleId);

public sealed record UserStatusRequest(bool IsActive);

internal sealed class SaveUserRequestValidator : AbstractValidator<SaveUserRequest>
{
    public SaveUserRequestValidator() => Include(new ErpUserProfileValidator());
}

internal sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        Include(new ErpUserProfileValidator());
        RuleFor(x => x.Password).NotEmpty().StrongPassword();
        RuleFor(x => x.BusinessUnitIds).NotEmpty();
    }
}

internal sealed class UserUnitsRequestValidator : AbstractValidator<UserUnitsRequest>
{
    public UserUnitsRequestValidator()
    {
        RuleFor(x => x.BusinessUnitIds).NotEmpty();
    }
}
