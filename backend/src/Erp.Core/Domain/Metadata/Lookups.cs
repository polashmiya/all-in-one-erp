namespace Erp.Core.Domain.Metadata;

public class LookupType : BaseEntity
{
    public required string Key { get; set; }

    public required string NameEn { get; set; }

    public string? NameBn { get; set; }

    public bool IsActive { get; set; } = true;
}

public class LookupValue : MasterRecord
{
    public Guid LookupTypeId { get; set; }

    public LookupType? LookupType { get; set; }

    public string? Code { get; set; }

    public required string NameEn { get; set; }

    public string? NameBn { get; set; }

    public int SortOrder { get; set; }

    public bool IsDefault { get; set; }
}
