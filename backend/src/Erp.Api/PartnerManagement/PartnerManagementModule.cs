using Erp.Api.PartnerManagement.Customers;
using Erp.Api.PartnerManagement.Suppliers;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.PartnerManagement;

public static class PartnerManagementModule
{
    public static IServiceCollection AddPartnerManagementModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, PartnerManagementMenu>();
        services.AddSingleton<IErpEndpoints, PartnerManagementEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, CustomerManifest>();
        services.AddSingleton<IFeatureManifestProvider, SupplierManifest>();
        services.AddSingleton<ILookupProvider, PartnerLookups>();
        return services;
    }
}

public sealed class PartnerManagementEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/partner-management").WithTags("PartnerManagement");
        CustomerEndpoints.Map(group);
        SupplierEndpoints.Map(group);
    }
}
