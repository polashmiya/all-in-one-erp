using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.Inventory.StockAdjustments;
using Erp.Api.Inventory.StockReports;
using Erp.Api.Inventory.StockTransfers;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.Inventory;

public static class StockModule
{
    public static readonly string[] Editors = [.. StockAdjustmentEndpoints.Editors, .. StockTransferEndpoints.Editors];

    public static readonly string[] Readers = [$"{InventoryMenu.StockBalance}.view", $"{InventoryMenu.StockLedger}.view", .. Editors];

    public static IServiceCollection AddStockModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, InventoryMenu>();
        services.AddSingleton<IErpEndpoints, StockEndpoints>();
        services.AddSingleton<IErpEndpoints, DocumentEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, StockAdjustmentManifest>();
        services.AddSingleton<IFeatureManifestProvider, StockTransferManifest>();
        services.AddSingleton<ILookupProvider, StockLookups>();
        services.AddSingleton<IApprovalSource, StockAdjustmentApprovalSource>();
        services.AddSingleton<IApprovalSource, StockTransferApprovalSource>();
        return services;
    }
}

public sealed class StockEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/inventory").WithTags("Inventory");
        StockAdjustmentEndpoints.Map(group);
        StockTransferEndpoints.Map(group);
        StockReportEndpoints.Map(group);
    }
}

public sealed class StockLookups : ILookupProvider
{
    public const string AdjustmentReason = "stock-adjustment-reason";

    public IEnumerable<LookupDef> GetLookups() =>
    [
        new(AdjustmentReason, new("Stock adjustment reason", "মজুত সমন্বয়ের কারণ"),
        [
            new("COUNT", new("Stock count", "মজুত গণনা")),
            new("DAMAGED", new("Damaged", "ক্ষতিগ্রস্ত")),
            new("EXPIRED", new("Expired", "মেয়াদোত্তীর্ণ")),
            new("LOST", new("Lost", "হারানো")),
            new("USED", new("Own use", "নিজস্ব ব্যবহার")),
        ]),
    ];
}
