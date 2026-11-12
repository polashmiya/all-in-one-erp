using System.Globalization;
using Erp.Core.Domain.Metadata;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Configuration;

public sealed class FeatureConfigurationService(ErpDbContext db, FeatureConfigService configs)
{
    private const int KeyLength = 50;
    private const int SortStep = 10;
    private const int CustomTextLength = 200;
    private const int CustomTextAreaLength = 2000;

    private sealed record Scope(
        Guid AccountId,
        Guid? BusinessUnitId,
        Guid FeatureId,
        string Code,
        Dictionary<Guid, FeatureSection> Sections,
        List<FeatureField> Fields,
        List<AccountFieldSetting> Settings,
        List<AccountSectionSetting> SectionSettings,
        Dictionary<string, List<int>> ListValues,
        Dictionary<string, string> LookupDefaults);

    public async Task<FeatureConfigEditor?> GetAsync(Guid accountId, Guid? businessUnitId, string code, CancellationToken ct) =>
        await LoadAsync(accountId, businessUnitId, code, ct) is { } scope ? Editor(scope) : null;

    public async Task<FeatureConfig?> PreviewAsync(Guid accountId, Guid? businessUnitId, string code, CancellationToken ct) =>
        await LoadAsync(accountId, businessUnitId, code, ct) is null
            ? null
            : (await configs.GetAsync(accountId, businessUnitId, code, ct))?.Config;

    private static FeatureConfigEditor Editor(Scope scope)
    {
        var sections = scope.Sections.Values
            .Where(s => s.ParentId is { } tab && scope.Sections.ContainsKey(tab))
            .OrderBy(s => scope.Sections[s.ParentId!.Value].SortOrder).ThenBy(s => s.SortOrder)
            .Select(s =>
            {
                var tab = scope.Sections[s.ParentId!.Value];
                return new ConfigSectionOption(s.Key, new(s.LabelEn, s.LabelBn), tab.Key, new(tab.LabelEn, tab.LabelBn));
            })
            .ToList();
        var order = sections.Select(s => s.Key).ToList();

        var fields = scope.Fields
            .OrderBy(f => order.IndexOf(scope.Sections[f.SectionId].Key)).ThenBy(f => f.SortOrder)
            .Select(f => Row(scope, f))
            .ToList();

        var layout = scope.Sections.Values.OrderBy(s => s.SortOrder).ThenBy(s => s.Key).Select(s => Row(scope, s)).ToList();

        return new FeatureConfigEditor(scope.Code, sections, fields, FieldTypes.Custom, layout);
    }

    public async Task<(SectionConfigRow? Row, Dictionary<string, string[]> Errors)> SaveSectionAsync(
        Guid accountId, Guid? businessUnitId, string code, string key, SectionSettingValues values, CancellationToken ct)
    {
        if (await LoadAsync(accountId, businessUnitId, code, ct) is not { } scope) return (null, []);
        if (scope.Sections.Values.FirstOrDefault(s => s.Key == key) is not { } section) return (null, []);

        var tab = values.TabKey is null ? null : TabOf(scope, values.TabKey);
        if (values.TabKey is not null && (section.ParentId is null || tab is null))
            return (null, new() { ["tabKey"] = ["Choose a tab of this screen."] });

        var before = OwnRow(scope, section) is { } old ? Values(old, scope) : null;
        var own = OwnRow(scope, section) ?? NewRow(scope, section);
        own.LabelEn = Trimmed(values.LabelEn);
        own.LabelBn = Trimmed(values.LabelBn);
        own.SortOrder = values.SortOrder;
        own.Columns = values.Columns;
        own.IsVisible = values.IsVisible;
        own.ParentId = tab?.Id;
        Keep(scope, own);

        if (HiddenProblem(scope) is { } problem) return (null, new() { ["isVisible"] = [problem] });
        Record(scope, ConfigurationAreas.Section, key, before, OwnRow(scope, section) is { } now ? Values(now, scope) : null);
        await CommitAsync(scope, ct);

        return (Row(scope, section), []);
    }

    public async Task<(FeatureConfigEditor? Editor, Dictionary<string, string[]> Errors)> ArrangeAsync(
        Guid accountId, Guid? businessUnitId, string code, LayoutRequest layout, CancellationToken ct)
    {
        if (await LoadAsync(accountId, businessUnitId, code, ct) is not { } scope) return (null, []);

        var tabs = scope.Sections.Values.Where(s => s.ParentId is null).ToDictionary(s => s.Key);
        var sections = scope.Sections.Values.Where(s => s.ParentId is not null).ToDictionary(s => s.Key);
        var fields = scope.Fields.ToDictionary(f => f.Key);
        var tabKeys = layout.Tabs.Select(t => t.Key).ToList();
        var sectionKeys = layout.Tabs.SelectMany(t => t.Sections).Select(s => s.Key).ToList();
        var fieldKeys = layout.Tabs.SelectMany(t => t.Sections).SelectMany(s => s.Fields).ToList();

        var known = tabKeys.All(tabs.ContainsKey) && sectionKeys.All(sections.ContainsKey) && fieldKeys.All(fields.ContainsKey);
        if (!known || Repeats(tabKeys) || Repeats(sectionKeys) || Repeats(fieldKeys))
            return (null, new() { ["tabs"] = ["The layout has unknown or repeated tabs, sections or fields."] });

        var before = LayoutOf(scope);
        foreach (var (tab, tabIndex) in layout.Tabs.Select((tab, index) => (tab, index)))
        {
            Place(scope, tabs[tab.Key], Order(tabIndex), null);
            foreach (var (section, sectionIndex) in tab.Sections.Select((section, index) => (section, index)))
            {
                Place(scope, sections[section.Key], Order(sectionIndex), tabs[tab.Key].Id);
                foreach (var (field, fieldIndex) in section.Fields.Select((field, index) => (field, index)))
                    Place(scope, fields[field], Order(fieldIndex), sections[section.Key].Id);
            }
        }

        if (HiddenProblem(scope) is { } problem) return (null, new() { ["tabs"] = [problem] });
        Record(scope, ConfigurationAreas.Layout, scope.Code, before, LayoutOf(scope));
        await CommitAsync(scope, ct);

        return (Editor(scope), []);
    }

    public async Task<(FieldConfigRow? Row, Dictionary<string, string[]> Errors)> CreateAsync(
        Guid accountId, string code, CreateCustomFieldRequest request, CancellationToken ct)
    {
        if (await LoadAsync(accountId, null, code, ct) is not { } scope) return (null, []);

        var errors = new Dictionary<string, string[]>();
        var key = CustomKey(request.LabelEn);
        var section = SectionOf(scope, request.SectionKey);
        var list = request.Type == FieldTypes.Lookup ? request.Lookup : null;

        if (key is null)
            errors["labelEn"] = ["Start the English label with a letter."];
        else if (await db.FeatureFields.AnyAsync(
            f => f.FeatureId == scope.FeatureId && f.Key == key && (f.AccountId == null || f.AccountId == accountId), ct))
            errors["labelEn"] = ["Another field of this screen already uses this name."];
        if (section is null) errors["sectionKey"] = ["Choose a section of this screen."];
        if (!FieldTypes.Custom.Contains(request.Type)) errors["type"] = ["Choose a field type."];
        if (request.Type == FieldTypes.Lookup && !await db.LookupTypes.AnyAsync(t => t.Key == list && t.IsActive, ct))
            errors["lookup"] = ["Choose a lookup list."];
        if (errors.Count > 0) return (null, errors);

        var field = new FeatureField
        {
            FeatureId = scope.FeatureId,
            AccountId = accountId,
            SectionId = section!.Id,
            Key = key!,
            DataType = request.Type,
            LookupSource = list,
            LabelEn = request.LabelEn.Trim(),
            LabelBn = Trimmed(request.LabelBn),
            SortOrder = scope.Fields.Where(f => f.SectionId == section.Id).Select(f => f.SortOrder).DefaultIfEmpty().Max() + SortStep,
            MaxLength = request.Type switch
            {
                FieldTypes.Text => CustomTextLength,
                FieldTypes.TextArea => CustomTextAreaLength,
                _ => null,
            },
        };
        db.FeatureFields.Add(field);
        scope.Fields.Add(field);
        Record(scope, ConfigurationAreas.CustomField, field.Key, null, request);
        await CommitAsync(scope, ct);

        return (Row(scope, field), []);
    }

    public async Task<(FieldConfigRow? Row, Dictionary<string, string[]> Errors)> SetStatusAsync(
        Guid accountId, string code, string key, bool isActive, CancellationToken ct)
    {
        if (await LoadAsync(accountId, null, code, ct) is not { } scope) return (null, []);
        if (scope.Fields.Find(f => f.Key == key && f.AccountId is not null) is not { } field) return (null, []);

        Record(scope, ConfigurationAreas.CustomField, key, new { field.IsActive }, new { IsActive = isActive });
        field.IsActive = isActive;
        await CommitAsync(scope, ct);

        return (Row(scope, field), []);
    }

    public async Task<(FieldConfigRow? Row, Dictionary<string, string[]> Errors)> SaveAsync(
        Guid accountId, Guid? businessUnitId, string code, string key, FieldSettingValues values, CancellationToken ct)
    {
        if (await LoadAsync(accountId, businessUnitId, code, ct) is not { } scope) return (null, []);
        if (scope.Fields.Find(f => f.Key == key) is not { } field) return (null, []);

        var errors = Validate(scope, field, values);
        if (errors.Count > 0) return (null, errors);

        var before = OwnRow(scope, field) is { } old ? Values(old, scope) : null;
        var own = OwnRow(scope, field) ?? NewRow(scope, field);
        Apply(own, values, scope);
        Keep(scope, own);

        if (HiddenProblem(scope) is { } problem) return (null, new() { ["sectionKey"] = [problem] });
        Record(scope, ConfigurationAreas.Field, key, before, OwnRow(scope, field) is { } now ? Values(now, scope) : null);
        await CommitAsync(scope, ct);

        return (Row(scope, field), []);
    }

    private void Place(Scope scope, FeatureSection section, int order, Guid? tabId)
    {
        var basis = BaseRow(scope, section);
        var own = OwnRow(scope, section) ?? NewRow(scope, section);
        own.SortOrder = order == basis.SortOrder ? null : order;
        own.ParentId = tabId == basis.ParentId ? null : tabId;
        Keep(scope, own);
    }

    private void Place(Scope scope, FeatureField field, int order, Guid sectionId)
    {
        var basis = BaseRow(scope, field);
        var own = OwnRow(scope, field) ?? NewRow(scope, field);
        own.SortOrder = order == basis.SortOrder ? null : order;
        own.SectionId = sectionId == basis.SectionId ? null : sectionId;
        Keep(scope, own);
    }

    private void Keep(Scope scope, AccountFieldSetting row)
    {
        var isNew = !scope.Settings.Contains(row);
        var isEmpty = Values(row, scope) == new FieldSettingValues();
        if (isEmpty && !isNew)
        {
            db.AccountFieldSettings.Remove(row);
            scope.Settings.Remove(row);
        }
        else if (!isEmpty && isNew)
        {
            db.AccountFieldSettings.Add(row);
            scope.Settings.Add(row);
        }
    }

    private void Keep(Scope scope, AccountSectionSetting row)
    {
        var isNew = !scope.SectionSettings.Contains(row);
        var isEmpty = Values(row, scope) == new SectionSettingValues();
        if (isEmpty && !isNew)
        {
            db.AccountSectionSettings.Remove(row);
            scope.SectionSettings.Remove(row);
        }
        else if (!isEmpty && isNew)
        {
            db.AccountSectionSettings.Add(row);
            scope.SectionSettings.Add(row);
        }
    }

    private void Record(Scope scope, string area, string key, object? before, object? after) =>
        ConfigurationHistory.Record(db, scope.AccountId, scope.BusinessUnitId, scope.Code, area, key, before, after);

    private static LayoutRequest LayoutOf(Scope scope)
    {
        var config = FeatureConfigBuilder.Build(
            scope.Code,
            0,
            scope.Sections.Values,
            scope.Fields,
            FeatureSettings.ForUnit(scope.SectionSettings, scope.BusinessUnitId),
            FeatureSettings.ForUnit(scope.Settings, scope.BusinessUnitId),
            scope.LookupDefaults).Config;
        return new LayoutRequest(config.Tabs
            .Select(t => new LayoutTab(t.Key, t.Sections.Select(s => new LayoutSection(s.Key, s.Fields.Select(f => f.Key).ToList())).ToList()))
            .ToList());
    }

    private async Task CommitAsync(Scope scope, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == scope.AccountId && af.FeatureId == scope.FeatureId)
            .ExecuteUpdateAsync(s => s.SetProperty(af => af.ConfigVersion, af => af.ConfigVersion + 1), ct);
    }

    private async Task<Scope?> LoadAsync(Guid accountId, Guid? businessUnitId, string code, CancellationToken ct)
    {
        var featureCode = code.ToLowerInvariant();
        var featureId = await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == accountId && af.Feature!.Code == featureCode && af.Feature.IsActive)
            .Select(af => (Guid?)af.FeatureId)
            .SingleOrDefaultAsync(ct);
        if (featureId is null) return null;

        if (businessUnitId is not null &&
            !await db.BusinessUnits.AcrossTenants().AnyAsync(b => b.Id == businessUnitId && b.AccountId == accountId, ct))
            return null;

        var fields = await db.FeatureFields
            .Where(f => f.FeatureId == featureId && ((f.AccountId == null && f.IsActive) || f.AccountId == accountId))
            .ToListAsync(ct);
        if (fields.Count == 0) return null;

        var sections = await db.FeatureSections.Where(s => s.FeatureId == featureId && s.IsActive).ToDictionaryAsync(s => s.Id, ct);
        var settings = await db.AccountFieldSettings.AcrossTenants()
            .Where(s => s.AccountId == accountId && s.Field!.FeatureId == featureId)
            .Where(s => s.BusinessUnitId == null || s.BusinessUnitId == businessUnitId)
            .ToListAsync(ct);
        var sectionSettings = await db.AccountSectionSettings.AcrossTenants()
            .Where(s => s.AccountId == accountId && s.Section!.FeatureId == featureId)
            .Where(s => s.BusinessUnitId == null || s.BusinessUnitId == businessUnitId)
            .ToListAsync(ct);

        var sources = fields.Select(f => f.LookupSource).OfType<string>().Distinct().ToList();
        var listValues = (await db.LookupValues.AcrossTenants()
                .Where(v => v.AccountId == accountId && v.IsActive && sources.Contains(v.LookupType!.Key))
                .Select(v => new { v.LookupType!.Key, v.Id })
                .ToListAsync(ct))
            .GroupBy(v => v.Key)
            .ToDictionary(g => g.Key, g => g.Select(v => v.Id).ToList());
        var lookupDefaults = await FeatureConfigService.LookupDefaultsAsync(db, accountId, ct);

        return new Scope(
            accountId, businessUnitId, featureId.Value, featureCode, sections, fields, settings, sectionSettings, listValues, lookupDefaults);
    }

    private static AccountFieldSetting NewRow(Scope scope, FeatureField field) =>
        new() { AccountId = scope.AccountId, BusinessUnitId = scope.BusinessUnitId, FieldId = field.Id };

    private static AccountSectionSetting NewRow(Scope scope, FeatureSection section) =>
        new() { AccountId = scope.AccountId, BusinessUnitId = scope.BusinessUnitId, SectionId = section.Id };

    private static AccountSectionSetting? OwnRow(Scope scope, FeatureSection section) =>
        scope.SectionSettings.Find(s => s.SectionId == section.Id && s.BusinessUnitId == scope.BusinessUnitId);

    private static AccountSectionSetting BaseRow(Scope scope, FeatureSection section)
    {
        var defaults = new AccountSectionSetting
        {
            SectionId = section.Id,
            ParentId = section.ParentId,
            LabelEn = section.LabelEn,
            LabelBn = section.LabelBn,
            SortOrder = section.SortOrder,
            Columns = section.Columns,
            IsVisible = true,
        };
        if (scope.BusinessUnitId is null) return defaults;

        var account = scope.SectionSettings.Find(s => s.SectionId == section.Id && s.BusinessUnitId is null);
        return FeatureSettings.Merge(defaults, account)!;
    }

    private static SectionConfigRow Row(Scope scope, FeatureSection section) => new(
        section.Key,
        section.ParentId is null,
        Values(BaseRow(scope, section), scope),
        OwnRow(scope, section) is { } own ? Values(own, scope) : null);

    private static SectionSettingValues Values(AccountSectionSetting row, Scope scope) => new()
    {
        LabelEn = row.LabelEn,
        LabelBn = row.LabelBn,
        SortOrder = row.SortOrder,
        Columns = row.Columns,
        IsVisible = row.IsVisible,
        TabKey = row.ParentId is { } id && scope.Sections.TryGetValue(id, out var tab) ? tab.Key : null,
    };

    private static string? HiddenProblem(Scope scope)
    {
        var hidden = scope.Fields.Where(f => f.IsActive).Where(field =>
        {
            var setting = FeatureSettings.Merge(BaseRow(scope, field), OwnRow(scope, field))!;
            if (!scope.Sections.TryGetValue(setting.SectionId ?? field.SectionId, out var section)) return false;

            var placed = FeatureSettings.Merge(BaseRow(scope, section), OwnRow(scope, section))!;
            var tabHidden = placed.ParentId is { } tabId && scope.Sections.TryGetValue(tabId, out var tab) &&
                FeatureSettings.Merge(BaseRow(scope, tab), OwnRow(scope, tab))!.IsVisible == false;
            if (placed.IsVisible != false && !tabHidden) return false;

            var hasDefault = setting.DefaultValue is not null ||
                (field.LookupSource is { } source && scope.LookupDefaults.ContainsKey(source));
            return field.IsLocked ||
                (setting.IsVisible != false && setting.IsRequired == true && !hasDefault && field.DataType != FieldTypes.Boolean);
        }).Select(f => f.LabelEn).ToList();

        return hidden.Count == 0
            ? null
            : $"These fields must stay visible: {string.Join(", ", hidden)}. Move them or make them optional first.";
    }

    private static FeatureSection? TabOf(Scope scope, string key) =>
        scope.Sections.Values.FirstOrDefault(s => s.Key == key && s.ParentId is null);

    private static bool Repeats(List<string> keys) => keys.Distinct().Count() != keys.Count;

    private static int Order(int index) => (index + 1) * SortStep;

    private static AccountFieldSetting? OwnRow(Scope scope, FeatureField field) =>
        scope.Settings.Find(s => s.FieldId == field.Id && s.BusinessUnitId == scope.BusinessUnitId);

    private static AccountFieldSetting BaseRow(Scope scope, FeatureField field)
    {
        var defaults = new AccountFieldSetting
        {
            FieldId = field.Id,
            SectionId = field.SectionId,
            IsVisible = true,
            IsRequired = field.IsRequired || field.IsLocked,
            IsReadOnly = false,
            LabelEn = field.LabelEn,
            LabelBn = field.LabelBn,
            HelpEn = field.HelpEn,
            HelpBn = field.HelpBn,
            PlaceholderEn = field.PlaceholderEn,
            PlaceholderBn = field.PlaceholderBn,
            SortOrder = field.SortOrder,
            ColSpan = field.ColSpan,
            DefaultValue = field.DefaultValue,
            MaxLength = field.MaxLength,
            MinValue = field.MinValue,
            MaxValue = field.MaxValue,
            InList = field.ListOrder is not null,
            ListOrder = field.ListOrder,
            InFilter = field.CanFilter,
        };
        if (scope.BusinessUnitId is null) return defaults;

        var account = scope.Settings.Find(s => s.FieldId == field.Id && s.BusinessUnitId is null);
        return FeatureSettings.Merge(defaults, account)!;
    }

    private static FieldConfigRow Row(Scope scope, FeatureField field) => new(
        field.Key,
        field.DataType,
        field.LookupSource,
        field.IsLocked,
        field.AccountId is not null,
        field.IsActive,
        field.ListOrder is not null,
        field.CanFilter,
        field.MaxLength,
        field.MinValue,
        field.MaxValue,
        Values(BaseRow(scope, field), scope),
        OwnRow(scope, field) is { } own ? Values(own, scope) : null);

    private static FieldSettingValues Values(AccountFieldSetting row, Scope scope) => new()
    {
        IsVisible = row.IsVisible,
        IsRequired = row.IsRequired,
        IsReadOnly = row.IsReadOnly,
        LabelEn = row.LabelEn,
        LabelBn = row.LabelBn,
        HelpEn = row.HelpEn,
        HelpBn = row.HelpBn,
        PlaceholderEn = row.PlaceholderEn,
        PlaceholderBn = row.PlaceholderBn,
        SectionKey = row.SectionId is { } id && scope.Sections.TryGetValue(id, out var section) ? section.Key : null,
        SortOrder = row.SortOrder,
        ColSpan = row.ColSpan,
        DefaultValue = row.DefaultValue,
        MaxLength = row.MaxLength,
        MinValue = row.MinValue,
        MaxValue = row.MaxValue,
        InList = row.InList,
        ListOrder = row.ListOrder,
        InFilter = row.InFilter,
    };

    private static void Apply(AccountFieldSetting row, FieldSettingValues values, Scope scope)
    {
        row.IsVisible = values.IsVisible;
        row.IsRequired = values.IsRequired;
        row.IsReadOnly = values.IsReadOnly;
        row.LabelEn = Trimmed(values.LabelEn);
        row.LabelBn = Trimmed(values.LabelBn);
        row.HelpEn = Trimmed(values.HelpEn);
        row.HelpBn = Trimmed(values.HelpBn);
        row.PlaceholderEn = Trimmed(values.PlaceholderEn);
        row.PlaceholderBn = Trimmed(values.PlaceholderBn);
        row.SectionId = values.SectionKey is null ? null : SectionOf(scope, values.SectionKey)!.Id;
        row.SortOrder = values.SortOrder;
        row.ColSpan = values.ColSpan;
        row.DefaultValue = Trimmed(values.DefaultValue);
        row.MaxLength = values.MaxLength;
        row.MinValue = values.MinValue;
        row.MaxValue = values.MaxValue;
        row.InList = values.InList;
        row.ListOrder = values.ListOrder;
        row.InFilter = values.InFilter;
    }

    private static Dictionary<string, string[]> Validate(Scope scope, FeatureField field, FieldSettingValues values)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string key, string message) => errors.TryAdd(key, [message]);

        if (field.IsLocked && values.IsVisible == false) Add("isVisible", "This field is always shown.");
        if (field.IsLocked && values.IsRequired == false) Add("isRequired", "This field is always required.");
        if (field.IsLocked && values.IsReadOnly == true) Add("isReadOnly", "This field cannot be read-only.");
        if (values.SectionKey is { } key && SectionOf(scope, key) is null) Add("sectionKey", "Choose a section of this screen.");
        if (field.MaxLength is { } cap && values.MaxLength > cap) Add("maxLength", $"Use {cap} or less.");
        if (field.MinValue is { } min && values.MinValue < min) Add("minValue", $"Use {Format(min)} or more.");
        if (field.MaxValue is { } max && values.MaxValue > max) Add("maxValue", $"Use {Format(max)} or less.");
        if (values.InList == true && field.ListOrder is null) Add("inList", "This field has no list column.");
        if (values.InFilter == true && !field.CanFilter) Add("inFilter", "This field cannot be a list filter.");
        if (Trimmed(values.DefaultValue) is { } value && DefaultProblem(scope, field, value) is { } problem) Add("defaultValue", problem);

        var current = BaseRow(scope, field);
        if ((values.MinValue ?? current.MinValue) is { } low && (values.MaxValue ?? current.MaxValue) is { } high && low > high)
            Add("maxValue", "Use a value above the minimum.");

        var hidden = (values.IsVisible ?? current.IsVisible) == false;
        var required = values.IsRequired ?? current.IsRequired ?? false;
        var hasDefault = Trimmed(values.DefaultValue) is not null || current.DefaultValue is not null ||
            (field.LookupSource is { } source && scope.LookupDefaults.ContainsKey(source));
        if (hidden && required && !hasDefault && field.DataType != FieldTypes.Boolean)
            Add("isVisible", "A required field needs a default value before it can be hidden. Add one or make it optional.");

        return errors;
    }

    private static string? DefaultProblem(Scope scope, FeatureField field, string value) => field.DataType switch
    {
        FieldTypes.Boolean => bool.TryParse(value, out _) ? null : "Use true or false.",
        FieldTypes.Lookup when field.LookupSource is { } source && scope.ListValues.TryGetValue(source, out var ids) =>
            int.TryParse(value, out var id) && ids.Contains(id) ? null : "Choose an active value from the list.",
        FieldTypes.Number or FieldTypes.Decimal or FieldTypes.Money or FieldTypes.Percent or FieldTypes.Lookup =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _) ? null : "Enter a number.",
        _ => field.MaxLength is { } cap && value.Length > cap ? $"Use at most {cap} characters." : null,
    };

    private static FeatureSection? SectionOf(Scope scope, string key) =>
        scope.Sections.Values.FirstOrDefault(s => s.Key == key && s.ParentId is not null);

    private static string? CustomKey(string label)
    {
        var words = label.Split(' ', '-', '_').Select(w => new string(w.Where(char.IsAsciiLetterOrDigit).ToArray())).Where(w => w.Length > 0);
        var key = string.Concat(words.Select((w, i) => i == 0 ? w.ToLowerInvariant() : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
        return key.Length > 0 && char.IsAsciiLetter(key[0]) ? key[..Math.Min(key.Length, KeyLength)] : null;
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Format(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
