using Erp.Core.Domain.Navigation;

namespace Erp.Core.Domain.Metadata;

public class FeatureSection : BaseEntity
{
    public Guid FeatureId { get; set; }

    public ErpFeature? Feature { get; set; }

    public required string Key { get; set; }

    public Guid? ParentId { get; set; }

    public FeatureSection? Parent { get; set; }

    public required string LabelEn { get; set; }

    public string? LabelBn { get; set; }

    public int SortOrder { get; set; }

    public int Columns { get; set; }

    public bool IsActive { get; set; } = true;
}

public class FeatureField : BaseEntity
{
    public Guid FeatureId { get; set; }

    public ErpFeature? Feature { get; set; }

    public Guid SectionId { get; set; }

    public FeatureSection? Section { get; set; }

    public required string Key { get; set; }

    public required string DataType { get; set; }

    public string? LookupSource { get; set; }

    public string? ParentFieldKey { get; set; }

    public required string LabelEn { get; set; }

    public string? LabelBn { get; set; }

    public string? HelpEn { get; set; }

    public string? HelpBn { get; set; }

    public string? PlaceholderEn { get; set; }

    public string? PlaceholderBn { get; set; }

    public int SortOrder { get; set; }

    public int ColSpan { get; set; } = 1;

    public bool IsLocked { get; set; }

    public bool IsRequired { get; set; }

    public int? MaxLength { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public int? Precision { get; set; }

    public string? DefaultValue { get; set; }

    public int? ListOrder { get; set; }

    public bool CanFilter { get; set; }

    public bool CanSort { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AccountSectionSetting : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Guid? BusinessUnitId { get; set; }

    public Guid SectionId { get; set; }

    public FeatureSection? Section { get; set; }

    public Guid? ParentId { get; set; }

    public string? LabelEn { get; set; }

    public string? LabelBn { get; set; }

    public int? SortOrder { get; set; }

    public int? Columns { get; set; }

    public bool? IsVisible { get; set; }
}

public class AccountFieldSetting : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Guid? BusinessUnitId { get; set; }

    public Guid FieldId { get; set; }

    public FeatureField? Field { get; set; }

    public Guid? SectionId { get; set; }

    public bool? IsVisible { get; set; }

    public bool? IsRequired { get; set; }

    public bool? IsReadOnly { get; set; }

    public string? LabelEn { get; set; }

    public string? LabelBn { get; set; }

    public string? HelpEn { get; set; }

    public string? HelpBn { get; set; }

    public string? PlaceholderEn { get; set; }

    public string? PlaceholderBn { get; set; }

    public int? SortOrder { get; set; }

    public int? ColSpan { get; set; }

    public string? DefaultValue { get; set; }

    public int? MaxLength { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public bool? InList { get; set; }

    public int? ListOrder { get; set; }

    public bool? InFilter { get; set; }
}
