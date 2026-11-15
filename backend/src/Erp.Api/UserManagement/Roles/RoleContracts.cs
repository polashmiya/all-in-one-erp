using Erp.Core.Modules;
using FluentValidation;

namespace Erp.Api.UserManagement.Roles;

public sealed record RoleRow(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsAdministrator,
    bool IsActive,
    int PermissionCount,
    int UsersInUnit);

public sealed record RoleCounts(int All, int Active, int Inactive);

public sealed record RoleList(IReadOnlyList<RoleRow> Items, int Total, RoleCounts Counts, int TotalPermissions);

public sealed record RoleOption(Guid Id, string Name, string? Description, bool IsAdministrator, int PermissionCount);

public sealed record RoleUserRow(Guid UserId, string FullName, string Username);

public sealed record RoleDetail(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsAdministrator,
    bool IsActive,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<RoleUserRow> Users);

public sealed record SaveRoleRequest(string Name, string? Description, bool IsActive, IReadOnlyList<string> Permissions);

public sealed record RoleStatusRequest(bool IsActive);

public sealed record RoleUsersRequest(IReadOnlyList<Guid> UserIds);

public sealed record PermissionTreeFeature(string Code, LocalizedLabel Name, IReadOnlyList<string> Actions);

public sealed record PermissionTreeSubModule(string Key, LocalizedLabel Name, string? Icon, IReadOnlyList<PermissionTreeFeature> Features);

public sealed record PermissionTreeModule(string Key, LocalizedLabel Name, string? Icon, IReadOnlyList<PermissionTreeSubModule> SubModules);

internal sealed class SaveRoleRequestValidator : AbstractValidator<SaveRoleRequest>
{
    public const int NameMax = 100;
    public const int DescriptionMax = 500;

    public SaveRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMax);
        RuleFor(x => x.Description).MaximumLength(DescriptionMax);
        RuleFor(x => x.Permissions).NotNull();
    }
}

internal sealed class RoleUsersRequestValidator : AbstractValidator<RoleUsersRequest>
{
    public RoleUsersRequestValidator()
    {
        RuleFor(x => x.UserIds).NotEmpty();
    }
}
