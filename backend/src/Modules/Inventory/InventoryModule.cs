using Erp.Core.Modules;
using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement;
using Erp.Modules.Inventory.ItemManagement.Items;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Modules.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, ItemManagementMenu>();
        services.AddSingleton<IFeatureManifestProvider, ItemProfileManifest>();
        services.AddSingleton<ILookupProvider, ItemLookups>();
        services.AddSingleton<IErpEndpoints, ItemManagementEndpoints>();
        services.AddValidatorsFromAssemblyContaining<ItemManagementEndpoints>(includeInternalTypes: true);
        return services;
    }
}
