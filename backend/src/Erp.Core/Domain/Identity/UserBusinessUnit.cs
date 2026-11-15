using Erp.Core.Domain.Tenancy;

namespace Erp.Core.Domain.Identity;

/// <summary>Gives an ERP user access to one business unit. The role is given later, inside that unit.</summary>
public class UserBusinessUnit : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Guid UserId { get; set; }

    public ErpUser? User { get; set; }

    public Guid BusinessUnitId { get; set; }

    public BusinessUnit? BusinessUnit { get; set; }

    public Guid? RoleId { get; set; }

    public Role? Role { get; set; }

    public bool IsActive { get; set; } = true;
}
