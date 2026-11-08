using Erp.Core.Domain.Navigation;

namespace Erp.Core.Domain.Tenancy;

/// <summary>A feature (menu screen) the Panel has given to an account.</summary>
public class AccountFeature : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Guid FeatureId { get; set; }

    public ErpFeature? Feature { get; set; }

    public int ConfigVersion { get; set; }
}
