using System.Globalization;
using Erp.Core.Domain.Metadata;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Configuration;

public sealed class FeatureConfigurationService(ErpDbContext db)
{
    private sealed record Scope(
        Guid AccountId,
        Guid? BusinessUnitId,
        Guid FeatureId,
        string Code,
        Dictionary<Guid, FeatureSection> Sections,
        List<FeatureField> Fields,
        List<AccountFieldSetting> Settings,
        Dictionary<string, List<int>> ListValues,
        Dictionary<string, string> LookupDefaults);

    public async Task<FeatureConfigEditor?> GetAsync(Guid accountId, Guid? businessUnitId, string code, CancellationToken ct)
    {
        if (await LoadAsync(accountId, businessUnitId, code, ct) is not { } scope) return null;

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

        return new FeatureConfigEditor(scope.Code, sections, fields);
    }

    public async Task<(FieldConfigRow? Row, Dictionary<string, string[]> Errors)> SaveAsync(
        Guid accountId, Guid? businessUnitId, string code, string key, FieldSettingValues values, CancellationToken ct)
    {
        if (await LoadAsync(accountId, businessUnitId, code, ct) is not { } scope) return (null, []);
        if (scope.Fields.Find(f => f.Key == key) is not { } field) return (null, []);

        var errors = Validate(scope, field, values);
        if (errors.Count > 0) return (null, errors);

        var own = OwnRow(scope, field);
        if (values == new FieldSettingValues())
        {
            if (own is not null)
            {
                db.AccountFieldSettings.Remove(own);
                scope.Settings.Remove(own);
            }
        }
        else
        {
            if (own is null)
            {
                own = new AccountFieldSetting { AccountId = accountId, BusinessUnitId = businessUnitId, FieldId = field.Id };
                db.AccountFieldSettings.Add(own);
                scope.Settings.Add(own);
            }

            Apply(own, values, scope);
        }

        await db.SaveChangesAsync(ct);
        await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == accountId && af.FeatureId == scope.FeatureId)
            .ExecuteUpdateAsync(s => s.SetProperty(af => af.ConfigVersion, af => af.ConfigVersion + 1), ct);

        return (Row(scope, field), []);
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

        var fields = await db.FeatureFields.Where(f => f.FeatureId == featureId && f.IsActive).ToListAsync(ct);
        if (fields.Count == 0) return null;

        var sections = await db.FeatureSections.Where(s => s.FeatureId == featureId && s.IsActive).ToDictionaryAsync(s => s.Id, ct);
        var settings = await db.AccountFieldSettings.AcrossTenants()
            .Where(s => s.AccountId == accountId && s.Field!.FeatureId == featureId)
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

        return new Scope(accountId, businessUnitId, featureId.Value, featureCode, sections, fields, settings, listValues, lookupDefaults);
    }

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

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Format(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
