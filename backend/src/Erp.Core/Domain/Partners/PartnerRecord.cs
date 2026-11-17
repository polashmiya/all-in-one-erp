namespace Erp.Core.Domain.Partners;

public abstract class PartnerRecord : MasterRecord, IBusinessUnitOwned
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public string? BillingName { get; set; }

    public int? GroupId { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public string? ContactPerson { get; set; }

    public string? ContactMobile { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public int? DistrictId { get; set; }

    public int? ThanaId { get; set; }

    public bool IsForeign { get; set; }

    public decimal? CreditLimit { get; set; }

    public int? PaymentDays { get; set; }

    public string? BinNumber { get; set; }

    public string? TinNumber { get; set; }

    public string? TradeLicenseNumber { get; set; }

    public string? Nid { get; set; }

    public string? CentralVatRegistrationNo { get; set; }

    public bool IsVatNotApplicable { get; set; }

    public bool IsVdsApplicable { get; set; }

    public string? Remarks { get; set; }

    public string? CustomData { get; set; }
}
