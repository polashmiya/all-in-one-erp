namespace Erp.Core.Domain.Partners;

public class Customer : PartnerRecord
{
    public int? PriceTypeId { get; set; }

    public int? TerritoryId { get; set; }

    public int? SalesRepresentativeId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public int? GenderId { get; set; }

    public string? Profession { get; set; }

    public DateOnly? MarriageDate { get; set; }
}
