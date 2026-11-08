using Erp.Core.Domain.Metadata;
using Erp.Core.Modules;

namespace Erp.Metadata;

public sealed record FeatureConfig(
    string Feature,
    int Version,
    IReadOnlyList<ConfigTab> Tabs,
    IReadOnlyList<ConfigColumn> Columns,
    IReadOnlyList<string> Filters);

public sealed record ConfigTab(string Key, LocalizedLabel Label, IReadOnlyList<ConfigSection> Sections);

public sealed record ConfigSection(string Key, LocalizedLabel Label, int Columns, IReadOnlyList<ConfigField> Fields);

public sealed record ConfigField(
    string Key,
    string Type,
    LocalizedLabel Label,
    LocalizedLabel? Help,
    LocalizedLabel? Placeholder,
    bool Required,
    bool ReadOnly,
    int ColSpan,
    string? DefaultValue,
    int? MaxLength,
    decimal? Min,
    decimal? Max,
    int? Precision,
    string? Lookup,
    string? ParentField);

public sealed record ConfigColumn(string Key, bool Sortable);

public sealed record FieldRule(ConfigField Field, bool Visible);

public sealed record ResolvedFeature(FeatureConfig Config, IReadOnlyList<FieldRule> Rules);

public static class FeatureConfigBuilder
{
    public static ResolvedFeature Build(
        string feature,
        int version,
        IReadOnlyCollection<FeatureSection> sections,
        IReadOnlyCollection<FeatureField> fields,
        IReadOnlyCollection<AccountSectionSetting> sectionSettings,
        IReadOnlyCollection<AccountFieldSetting> fieldSettings)
    {
        var sectionSetting = sectionSettings.ToDictionary(s => s.SectionId);
        var fieldSetting = fieldSettings.ToDictionary(s => s.FieldId);
        var active = sections.Where(s => s.IsActive).ToDictionary(s => s.Id);
        var tabs = active.Values.Where(s => s.ParentId is null).ToDictionary(s => s.Id);

        bool Hidden(FeatureSection section) => sectionSetting.GetValueOrDefault(section.Id)?.IsVisible == false;

        FeatureSection TabOf(FeatureSection section) =>
            sectionSetting.GetValueOrDefault(section.Id)?.ParentId is { } moved && tabs.TryGetValue(moved, out var tab)
                ? tab
                : tabs[section.ParentId!.Value];

        int OrderOf(FeatureSection section) => sectionSetting.GetValueOrDefault(section.Id)?.SortOrder ?? section.SortOrder;

        var placed = fields
            .Where(f => f.IsActive && active.ContainsKey(f.SectionId))
            .Select(f =>
            {
                var setting = fieldSetting.GetValueOrDefault(f.Id);
                var section = setting?.SectionId is { } moved && active.TryGetValue(moved, out var target) && target.ParentId is not null
                    ? target
                    : active[f.SectionId];
                var tab = TabOf(section);
                var visible = f.IsLocked || (setting?.IsVisible != false && !Hidden(section) && !Hidden(tab));
                return (Field: f, Setting: setting, Section: section, Tab: tab, Visible: visible, Config: Field(f, setting));
            })
            .ToList();

        var shown = placed
            .Where(x => x.Visible)
            .OrderBy(x => OrderOf(x.Tab)).ThenBy(x => x.Tab.Key)
            .ThenBy(x => OrderOf(x.Section)).ThenBy(x => x.Section.Key)
            .ThenBy(x => x.Setting?.SortOrder ?? x.Field.SortOrder).ThenBy(x => x.Field.Key)
            .ToList();

        var layout = shown
            .GroupBy(x => x.Tab)
            .Select(tab => new ConfigTab(
                tab.Key.Key,
                Label(tab.Key, sectionSetting.GetValueOrDefault(tab.Key.Id)),
                tab.GroupBy(x => x.Section)
                    .Select(section => new ConfigSection(
                        section.Key.Key,
                        Label(section.Key, sectionSetting.GetValueOrDefault(section.Key.Id)),
                        sectionSetting.GetValueOrDefault(section.Key.Id)?.Columns ?? section.Key.Columns,
                        section.Select(x => x.Config).ToList()))
                    .ToList()))
            .ToList();

        var columns = shown
            .Where(x => x.Setting?.InList ?? x.Field.ListOrder is not null)
            .OrderBy(x => x.Setting?.ListOrder ?? x.Field.ListOrder ?? int.MaxValue)
            .Select(x => new ConfigColumn(x.Field.Key, x.Field.CanSort))
            .ToList();

        var filters = shown
            .Where(x => x.Field.CanFilter && x.Setting?.InFilter != false)
            .Select(x => x.Field.Key)
            .ToList();

        return new ResolvedFeature(
            new FeatureConfig(feature, version, layout, columns, filters),
            placed.Select(x => new FieldRule(x.Config, x.Visible)).ToList());
    }

    private static LocalizedLabel Label(FeatureSection section, AccountSectionSetting? setting) =>
        new(setting?.LabelEn ?? section.LabelEn, setting?.LabelBn ?? section.LabelBn);

    private static ConfigField Field(FeatureField field, AccountFieldSetting? setting) => new(
        field.Key,
        field.DataType,
        new LocalizedLabel(setting?.LabelEn ?? field.LabelEn, setting?.LabelBn ?? field.LabelBn),
        Text(setting?.HelpEn ?? field.HelpEn, setting?.HelpBn ?? field.HelpBn),
        Text(setting?.PlaceholderEn ?? field.PlaceholderEn, setting?.PlaceholderBn ?? field.PlaceholderBn),
        field.IsLocked || (setting?.IsRequired ?? field.IsRequired),
        !field.IsLocked && setting?.IsReadOnly == true,
        setting?.ColSpan ?? field.ColSpan,
        setting?.DefaultValue ?? field.DefaultValue,
        Tighter(field.MaxLength, setting?.MaxLength, Math.Min),
        Tighter(field.MinValue, setting?.MinValue, Math.Max),
        Tighter(field.MaxValue, setting?.MaxValue, Math.Min),
        field.Precision,
        field.LookupSource,
        field.ParentFieldKey);

    private static LocalizedLabel? Text(string? en, string? bn) => en is null ? null : new(en, bn);

    private static T? Tighter<T>(T? limit, T? value, Func<T, T, T> pick) where T : struct =>
        limit is { } l && value is { } v ? pick(l, v) : value ?? limit;
}
