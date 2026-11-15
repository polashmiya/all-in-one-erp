namespace Erp.Core.Domain;

/// <summary>An unfinished create form, saved as typed so it can be finished later.</summary>
public class Draft : BaseEntity
{
    public required string Surface { get; set; }

    public Guid? AccountId { get; set; }

    public Guid? BusinessUnitId { get; set; }

    public required string Feature { get; set; }

    public required string Title { get; set; }

    public required string Data { get; set; }
}
