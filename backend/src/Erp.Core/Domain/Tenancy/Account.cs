namespace Erp.Core.Domain.Tenancy;

/// <summary>A client company. Everything a client owns hangs off its account.</summary>
public class Account : BaseEntity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Company contact email (not a login).</summary>
    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    /// <summary>Descriptive only; behaviour never branches on it.</summary>
    public string? Industry { get; set; }

    /// <summary>BIN / TIN / VAT registration number.</summary>
    public string? TaxId { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    /// <summary>ISO 3166-1 alpha-2, e.g. BD.</summary>
    public string? Country { get; set; }

    /// <summary>ISO 4217, e.g. BDT.</summary>
    public string BaseCurrency { get; set; } = "BDT";

    /// <summary>IANA time zone, e.g. Asia/Dhaka.</summary>
    public string TimeZone { get; set; } = "Asia/Dhaka";

    public bool IsActive { get; set; } = true;

    public string? Theme { get; set; }

    public ICollection<BusinessUnit> BusinessUnits { get; } = [];
}
