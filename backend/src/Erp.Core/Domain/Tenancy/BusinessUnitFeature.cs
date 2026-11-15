using Erp.Core.Domain.Navigation;

namespace Erp.Core.Domain.Tenancy;

/// <summary>A feature of the account that the Panel turned off for one business unit.</summary>
public class BusinessUnitFeature : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Guid BusinessUnitId { get; set; }

    public Guid FeatureId { get; set; }

    public ErpFeature? Feature { get; set; }
}
