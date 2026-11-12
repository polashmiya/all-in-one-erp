using System.Text.Json;
using FluentValidation;

namespace Erp.Api.Panel.Accounts;

public sealed record UpdateAccountRequest(AccountInfoInput Account, bool IsActive);

public static class StructureKinds
{
    public const string BusinessUnit = "business-unit";
    public const string Office = "office";
    public const string Warehouse = "warehouse";

    public static readonly IReadOnlyList<string> All = [BusinessUnit, Office, Warehouse];
}

public sealed record StructureNodeRequest(
    string Kind,
    Guid? BusinessUnitId,
    Guid? OfficeId,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive);

public sealed record StructureNodeResult(Guid Id);

public sealed record AccountTheme(string Preset, string? CustomPrimary, string Sider, string Mode, bool Compact)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static AccountTheme? Parse(string? json) =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<AccountTheme>(json, Json);

    public string ToJson() => JsonSerializer.Serialize(this, Json);
}

internal sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator() => RuleFor(x => x.Account).NotNull().SetValidator(new AccountInfoValidator());
}

internal sealed class StructureNodeRequestValidator : AbstractValidator<StructureNodeRequest>
{
    public StructureNodeRequestValidator()
    {
        RuleFor(x => x.Kind).Must(StructureKinds.All.Contains).WithMessage("Choose a business unit, office or warehouse.");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(Rules.Code).WithMessage(Rules.CodeMessage);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(32).Matches(Rules.Phone).WithMessage(Rules.PhoneMessage).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

internal sealed class AccountThemeValidator : AbstractValidator<AccountTheme>
{
    private static readonly string[] Siders = ["light", "dark", "brand"];
    private static readonly string[] Modes = ["light", "dark", "system"];

    public AccountThemeValidator()
    {
        RuleFor(x => x.Preset).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CustomPrimary).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Use a colour like #0F766E.")
            .When(x => x.CustomPrimary is not null);
        RuleFor(x => x.Sider).Must(Siders.Contains).WithMessage("Choose a sidebar style.");
        RuleFor(x => x.Mode).Must(Modes.Contains).WithMessage("Choose light, dark or system.");
    }
}
