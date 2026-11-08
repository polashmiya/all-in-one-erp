namespace Erp.Core.Domain.Tenancy;

/// <summary>A division of an account (e.g. a depot or company unit).</summary>
public class BusinessUnit : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Account? Account { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Office> Offices { get; } = [];

    public ICollection<Warehouse> Warehouses { get; } = [];
}
