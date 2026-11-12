using Erp.Core.Modules;

namespace Erp.Metadata;

public static class FieldTypes
{
    public const string Text = "text";
    public const string TextArea = "textarea";
    public const string Number = "number";
    public const string Decimal = "decimal";
    public const string Money = "money";
    public const string Percent = "percent";
    public const string Date = "date";
    public const string DateTime = "datetime";
    public const string Boolean = "boolean";
    public const string Lookup = "lookup";
    public const string Image = "image";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Text, TextArea, Number, Decimal, Money, Percent, Date, DateTime, Boolean, Lookup, Image };

    public static readonly IReadOnlyList<string> Custom = [Text, TextArea, Number, Decimal, Money, Percent, Date, Boolean, Lookup];
}

public sealed record SectionDef(string Key, MenuText Label, int Order, string? Tab = null, int Columns = 4);

public sealed record FieldDef(string Key, string Type, MenuText Label, string Section, int Order)
{
    public bool Locked { get; init; }

    public bool Required { get; init; }

    public int ColSpan { get; init; } = 1;

    public MenuText? Help { get; init; }

    public MenuText? Placeholder { get; init; }

    public int? MaxLength { get; init; }

    public decimal? Min { get; init; }

    public decimal? Max { get; init; }

    public int? Precision { get; init; }

    public string? Lookup { get; init; }

    public string? ParentField { get; init; }

    public string? Default { get; init; }

    public int? ListOrder { get; init; }

    public bool Filter { get; init; }

    public bool Sort { get; init; }
}

public sealed record FeatureManifest(string Feature, IReadOnlyList<SectionDef> Sections, IReadOnlyList<FieldDef> Fields);

public interface IFeatureManifestProvider
{
    IEnumerable<FeatureManifest> GetManifests();
}
