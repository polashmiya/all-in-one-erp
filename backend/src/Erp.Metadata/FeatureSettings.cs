using Erp.Core.Domain.Metadata;

namespace Erp.Metadata;

public static class FeatureSettings
{
    public static List<AccountFieldSetting> ForUnit(IEnumerable<AccountFieldSetting> rows, Guid? businessUnitId) =>
        rows.GroupBy(r => r.FieldId)
            .Select(g => Merge(g.FirstOrDefault(r => r.BusinessUnitId is null), UnitRow(g, r => r.BusinessUnitId, businessUnitId)))
            .OfType<AccountFieldSetting>()
            .ToList();

    public static List<AccountSectionSetting> ForUnit(IEnumerable<AccountSectionSetting> rows, Guid? businessUnitId) =>
        rows.GroupBy(r => r.SectionId)
            .Select(g => Merge(g.FirstOrDefault(r => r.BusinessUnitId is null), UnitRow(g, r => r.BusinessUnitId, businessUnitId)))
            .OfType<AccountSectionSetting>()
            .ToList();

    public static AccountFieldSetting? Merge(AccountFieldSetting? account, AccountFieldSetting? unit) =>
        account is null || unit is null ? unit ?? account : new AccountFieldSetting
        {
            AccountId = unit.AccountId,
            BusinessUnitId = unit.BusinessUnitId,
            FieldId = unit.FieldId,
            SectionId = unit.SectionId ?? account.SectionId,
            IsVisible = unit.IsVisible ?? account.IsVisible,
            IsRequired = unit.IsRequired ?? account.IsRequired,
            IsReadOnly = unit.IsReadOnly ?? account.IsReadOnly,
            LabelEn = unit.LabelEn ?? account.LabelEn,
            LabelBn = unit.LabelBn ?? account.LabelBn,
            HelpEn = unit.HelpEn ?? account.HelpEn,
            HelpBn = unit.HelpBn ?? account.HelpBn,
            PlaceholderEn = unit.PlaceholderEn ?? account.PlaceholderEn,
            PlaceholderBn = unit.PlaceholderBn ?? account.PlaceholderBn,
            SortOrder = unit.SortOrder ?? account.SortOrder,
            ColSpan = unit.ColSpan ?? account.ColSpan,
            DefaultValue = unit.DefaultValue ?? account.DefaultValue,
            MaxLength = unit.MaxLength ?? account.MaxLength,
            MinValue = unit.MinValue ?? account.MinValue,
            MaxValue = unit.MaxValue ?? account.MaxValue,
            InList = unit.InList ?? account.InList,
            ListOrder = unit.ListOrder ?? account.ListOrder,
            InFilter = unit.InFilter ?? account.InFilter,
        };

    public static AccountSectionSetting? Merge(AccountSectionSetting? account, AccountSectionSetting? unit) =>
        account is null || unit is null ? unit ?? account : new AccountSectionSetting
        {
            AccountId = unit.AccountId,
            BusinessUnitId = unit.BusinessUnitId,
            SectionId = unit.SectionId,
            ParentId = unit.ParentId ?? account.ParentId,
            LabelEn = unit.LabelEn ?? account.LabelEn,
            LabelBn = unit.LabelBn ?? account.LabelBn,
            SortOrder = unit.SortOrder ?? account.SortOrder,
            Columns = unit.Columns ?? account.Columns,
            IsVisible = unit.IsVisible ?? account.IsVisible,
        };

    private static T? UnitRow<T>(IEnumerable<T> rows, Func<T, Guid?> unitOf, Guid? businessUnitId) where T : class =>
        businessUnitId is null ? null : rows.FirstOrDefault(r => unitOf(r) == businessUnitId);
}
