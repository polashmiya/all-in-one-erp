using Erp.Core.Modules;
using FluentValidation;

namespace Erp.Api.Panel.Configuration;

public sealed record FieldSettingValues
{
    public bool? IsVisible { get; init; }
    public bool? IsRequired { get; init; }
    public bool? IsReadOnly { get; init; }
    public string? LabelEn { get; init; }
    public string? LabelBn { get; init; }
    public string? HelpEn { get; init; }
    public string? HelpBn { get; init; }
    public string? PlaceholderEn { get; init; }
    public string? PlaceholderBn { get; init; }
    public string? SectionKey { get; init; }
    public int? SortOrder { get; init; }
    public int? ColSpan { get; init; }
    public string? DefaultValue { get; init; }
    public int? MaxLength { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public bool? InList { get; init; }
    public int? ListOrder { get; init; }
    public bool? InFilter { get; init; }
}

public sealed record ConfigSectionOption(string Key, LocalizedLabel Label, string TabKey, LocalizedLabel TabLabel);

public sealed record FieldConfigRow(
    string Key,
    string Type,
    string? Lookup,
    bool IsLocked,
    bool IsCustom,
    bool IsActive,
    bool CanList,
    bool CanFilter,
    int? MaxLengthLimit,
    decimal? MinLimit,
    decimal? MaxLimit,
    FieldSettingValues Base,
    FieldSettingValues? Override);

public sealed record FeatureConfigEditor(
    string Feature,
    IReadOnlyList<ConfigSectionOption> Sections,
    IReadOnlyList<FieldConfigRow> Fields,
    IReadOnlyList<string> CustomTypes,
    IReadOnlyList<SectionConfigRow> Layout);

public sealed record SectionSettingValues
{
    public string? LabelEn { get; init; }
    public string? LabelBn { get; init; }
    public int? SortOrder { get; init; }
    public int? Columns { get; init; }
    public bool? IsVisible { get; init; }
    public string? TabKey { get; init; }
}

public sealed record SectionConfigRow(string Key, bool IsTab, SectionSettingValues Base, SectionSettingValues? Override);

public sealed record LayoutSection(string Key, IReadOnlyList<string> Fields);

public sealed record LayoutTab(string Key, IReadOnlyList<LayoutSection> Sections);

public sealed record LayoutRequest(IReadOnlyList<LayoutTab> Tabs);

internal sealed class SectionSettingValuesValidator : AbstractValidator<SectionSettingValues>
{
    public SectionSettingValuesValidator()
    {
        RuleFor(x => x.LabelEn).MaximumLength(100);
        RuleFor(x => x.LabelBn).MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Columns).InclusiveBetween(1, 4);
    }
}

public sealed record CreateCustomFieldRequest(string LabelEn, string? LabelBn, string Type, string SectionKey, string? Lookup);

public sealed record CustomFieldStatusRequest(bool IsActive);

internal sealed class CreateCustomFieldRequestValidator : AbstractValidator<CreateCustomFieldRequest>
{
    public CreateCustomFieldRequestValidator()
    {
        RuleFor(x => x.LabelEn).NotEmpty().WithMessage("Enter the English label.").MaximumLength(100);
        RuleFor(x => x.LabelBn).MaximumLength(100);
    }
}

internal sealed class FieldSettingValuesValidator : AbstractValidator<FieldSettingValues>
{
    public FieldSettingValuesValidator()
    {
        RuleFor(x => x.LabelEn).MaximumLength(100);
        RuleFor(x => x.LabelBn).MaximumLength(100);
        RuleFor(x => x.HelpEn).MaximumLength(300);
        RuleFor(x => x.HelpBn).MaximumLength(300);
        RuleFor(x => x.PlaceholderEn).MaximumLength(100);
        RuleFor(x => x.PlaceholderBn).MaximumLength(100);
        RuleFor(x => x.DefaultValue).MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ListOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ColSpan).InclusiveBetween(1, 4);
        RuleFor(x => x.MaxLength).GreaterThan(0);
    }
}
