using Erp.Api.Auth;
using FluentValidation;

namespace Erp.Api.Panel.Accounts;

/// <summary>
/// Shape and format rules for the setup request. Error keys follow the JSON path
/// (e.g. <c>businessUnits[1].offices[0].code</c>) so the form can attach them to the right field.
/// Database checks (code / email already taken) happen in <see cref="AccountSetupService"/>.
/// </summary>
internal sealed class AccountSetupRequestValidator : AbstractValidator<AccountSetupRequest>
{
    public AccountSetupRequestValidator()
    {
        RuleFor(x => x.Account).NotNull().SetValidator(new AccountInfoValidator());
        RuleFor(x => x.Admin).NotNull().SetValidator(new AdminUserValidator());
        RuleFor(x => x.FeatureCodes).NotNull();

        RuleFor(x => x.BusinessUnits).NotNull().NotEmpty().WithMessage("Add at least one business unit.");
        RuleForEach(x => x.BusinessUnits).SetValidator(new BusinessUnitValidator());
        RuleFor(x => x.BusinessUnits).Custom(CheckCodesAndReferences).When(x => x.BusinessUnits is not null);
    }

    /// <summary>Codes must be unique among siblings, and a warehouse's office must be one of its unit's offices.</summary>
    private static void CheckCodesAndReferences(IReadOnlyList<BusinessUnitInput> units, ValidationContext<AccountSetupRequest> context)
    {
        FlagDuplicates(units.Select(u => u.Code), i => $"businessUnits[{i}].code", "Another business unit already uses this code.", context);

        for (var b = 0; b < units.Count; b++)
        {
            var offices = units[b].Offices ?? [];
            var warehouses = units[b].Warehouses ?? [];

            FlagDuplicates(offices.Select(o => o.Code), i => $"businessUnits[{b}].offices[{i}].code", "Another office in this business unit uses this code.", context);
            FlagDuplicates(warehouses.Select(w => w.Code), i => $"businessUnits[{b}].warehouses[{i}].code", "Another warehouse in this business unit uses this code.", context);

            var officeCodes = offices.Select(o => Normalize(o.Code)).ToHashSet();
            for (var w = 0; w < warehouses.Count; w++)
            {
                var officeCode = warehouses[w].OfficeCode;
                if (!string.IsNullOrWhiteSpace(officeCode) && !officeCodes.Contains(Normalize(officeCode)))
                {
                    context.AddFailure($"businessUnits[{b}].warehouses[{w}].officeCode", "Choose an office of this business unit.");
                }
            }
        }
    }

    private static void FlagDuplicates(
        IEnumerable<string?> codes, Func<int, string> path, string message, ValidationContext<AccountSetupRequest> context)
    {
        var list = codes.Select(Normalize).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Length > 0 && list.IndexOf(list[i]) != i) context.AddFailure(path(i), message);
        }
    }

    internal static string Normalize(string? code) => code?.Trim().ToUpperInvariant() ?? "";
}

internal sealed class AccountInfoValidator : AbstractValidator<AccountInfoInput>
{
    public AccountInfoValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(2, 32).Matches(Rules.Code).WithMessage(Rules.CodeMessage);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32).Matches(Rules.Phone).WithMessage(Rules.PhoneMessage);
        RuleFor(x => x.Website).MaximumLength(200).Must(Rules.BeHttpUrl!).WithMessage("Enter a full web address, e.g. https://example.com.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.TaxId).MaximumLength(50);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().Matches("^[A-Za-z]{2}$").WithMessage("Use a two-letter country code.");
        RuleFor(x => x.BaseCurrency).NotEmpty().Matches("^[A-Za-z]{3}$").WithMessage("Use a three-letter currency code.");
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(64)
            .Must(tz => TimeZoneInfo.TryFindSystemTimeZoneById(tz, out _)).WithMessage("Unknown time zone.");
    }
}

internal sealed class BusinessUnitValidator : AbstractValidator<BusinessUnitInput>
{
    public BusinessUnitValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(Rules.Code).WithMessage(Rules.CodeMessage);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(32).Matches(Rules.Phone).WithMessage(Rules.PhoneMessage).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Offices).NotNull();
        RuleFor(x => x.Warehouses).NotNull();
        RuleForEach(x => x.Offices).SetValidator(new OfficeValidator());
        RuleForEach(x => x.Warehouses).SetValidator(new WarehouseValidator());
    }
}

internal sealed class OfficeValidator : AbstractValidator<OfficeInput>
{
    public OfficeValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(Rules.Code).WithMessage(Rules.CodeMessage);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(32).Matches(Rules.Phone).WithMessage(Rules.PhoneMessage).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

internal sealed class WarehouseValidator : AbstractValidator<WarehouseInput>
{
    public WarehouseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(Rules.Code).WithMessage(Rules.CodeMessage);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

internal sealed class AdminUserValidator : AbstractValidator<AdminUserInput>
{
    public AdminUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Username).ValidUsername();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password!).StrongPassword().When(x => !string.IsNullOrEmpty(x.Password));
    }
}

internal static class Rules
{
    /// <summary>Letters, digits and dashes; starts with a letter or digit. Stored upper-case.</summary>
    public const string Code = "^[A-Za-z0-9][A-Za-z0-9-]*$";
    public const string CodeMessage = "Use letters, digits and dashes only (no spaces).";

    public const string Phone = @"^\+?[0-9][0-9 \-]{5,19}$";
    public const string PhoneMessage = "Enter a valid phone number, e.g. +8801712345678.";

    public static bool BeHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
