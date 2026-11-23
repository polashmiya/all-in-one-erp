using Erp.Api.Procurement.PurchaseOrders;
using Erp.Api.Procurement.PurchaseRequisitions;
using Erp.Core.Authorization;
using Erp.Core.Modules;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;

namespace Erp.Api.Procurement;

public static class ProcurementModule
{
    public static readonly string[] Editors = [.. PurchaseRequisitionEndpoints.Editors, .. PurchaseOrderEndpoints.Editors];

    public static IServiceCollection AddProcurementModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, ProcurementMenu>();
        services.AddSingleton<IErpEndpoints, ProcurementEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, PurchaseRequisitionManifest>();
        services.AddSingleton<IFeatureManifestProvider, PurchaseOrderManifest>();
        services.AddSingleton<ILookupProvider, ProcurementLookups>();
        return services;
    }
}

public sealed class ProcurementEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/procurement").WithTags("Procurement");

        group.MapGet("/items", (string? search, ErpDbContext db, CancellationToken cancellationToken) =>
                ProcurementData.ItemsAsync(db, search, cancellationToken))
            .WithName("GetProcurementItemOptions")
            .WithSummary("Up to 20 active items matching the search, with their UOM, last cost and purchase VAT")
            .RequireAnyPermission(ProcurementModule.Editors);

        PurchaseRequisitionEndpoints.Map(group);
        PurchaseOrderEndpoints.Map(group);
    }
}
