using Erp.Api.Approvals;
using Erp.Api.Sales.SalesQuotations;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.Sales;

public static class SalesModule
{
    public static readonly string[] Editors = [.. SalesQuotationEndpoints.Editors];

    public static IServiceCollection AddSalesModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, SalesMenu>();
        services.AddSingleton<IErpEndpoints, SalesEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, SalesQuotationManifest>();
        services.AddSingleton<ILookupProvider, SalesLookups>();
        services.AddSingleton<IApprovalSource, QuotationApprovalSource>();
        return services;
    }
}

public sealed class SalesEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/sales").WithTags("Sales");

        SalesQuotationEndpoints.Map(group);
    }
}
