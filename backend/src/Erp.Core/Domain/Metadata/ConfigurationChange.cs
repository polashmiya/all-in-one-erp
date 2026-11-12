namespace Erp.Core.Domain.Metadata;

public static class ConfigurationAreas
{
    public const string Field = "field";
    public const string Section = "section";
    public const string Layout = "layout";
    public const string CustomField = "custom-field";
    public const string LookupValue = "lookup-value";
    public const string Theme = "theme";
}

public class ConfigurationChange : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Guid? BusinessUnitId { get; set; }

    public string? Feature { get; set; }

    public required string Area { get; set; }

    public required string Key { get; set; }

    public string? Before { get; set; }

    public string? After { get; set; }
}
