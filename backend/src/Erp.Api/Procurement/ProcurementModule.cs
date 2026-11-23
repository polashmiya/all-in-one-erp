using Erp.Api.Procurement.PurchaseRequisitions;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.Procurement;

public static class ProcurementModule
{
    public static IServiceCollection AddProcurementModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, ProcurementMenu>();
        services.AddSingleton<IErpEndpoints, ProcurementEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, PurchaseRequisitionManifest>();
        services.AddSingleton<ILookupProvider, ProcurementLookups>();
        return services;
    }
}

public sealed class ProcurementEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/procurement").WithTags("Procurement");
        PurchaseRequisitionEndpoints.Map(group);
    }
}
