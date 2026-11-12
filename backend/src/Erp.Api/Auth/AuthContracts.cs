using Erp.Api.Panel.Accounts;
using FluentValidation;

namespace Erp.Api.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record PanelUserDto(Guid Id, string Email, string FullName, string Role);

public sealed record PanelLoginResponse(string AccessToken, DateTimeOffset ExpiresAt, PanelUserDto User);

public sealed record ErpUserDto(Guid Id, string Email, string FullName, Guid AccountId, string AccountName, bool IsPrimaryAdmin);

public sealed record BusinessUnitAccessDto(Guid Id, string Code, string Name, string RoleName);

/// <summary>Login result. The token is account-scoped; the active business unit travels in X-Business-Unit.</summary>
public sealed record ErpLoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    ErpUserDto User,
    IReadOnlyList<BusinessUnitAccessDto> BusinessUnits,
    Guid? LastBusinessUnitId,
    AccountTheme? Theme);

public sealed record ErpSessionResponse(
    ErpUserDto User,
    IReadOnlyList<BusinessUnitAccessDto> BusinessUnits,
    Guid? LastBusinessUnitId);

public sealed record SelectUnitRequest(Guid BusinessUnitId);

public sealed record SelectUnitResponse(BusinessUnitAccessDto BusinessUnit, IReadOnlyList<string> Permissions);

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

internal sealed class SelectUnitRequestValidator : AbstractValidator<SelectUnitRequest>
{
    public SelectUnitRequestValidator()
    {
        RuleFor(x => x.BusinessUnitId).NotEmpty();
    }
}
