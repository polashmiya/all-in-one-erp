using Erp.Api.Approvals;
using Erp.Api.Sales.SalesCollections;
using Erp.Api.Sales.SalesDeliveries;
using Erp.Api.Sales.SalesOrders;
using Erp.Api.Sales.SalesQuotations;
using Erp.Api.Sales.SalesReports;
using Erp.Api.Sales.SalesReturns;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.Sales;

public static class SalesModule
{
    public static readonly string[] Editors =
        [
            .. SalesQuotationEndpoints.Editors,
            .. SalesOrderEndpoints.Editors,
            .. SalesDeliveryEndpoints.Editors,
            .. SalesReturnEndpoints.Editors,
            .. SalesCollectionEndpoints.Editors,
        ];

    public static readonly string[] Readers =
        [
            .. Editors,
            $"{SalesMenu.SalesReport}.view",
            $"{SalesMenu.CollectionReport}.view",
            $"{SalesMenu.CustomerDue}.view",
        ];

    public static IServiceCollection AddSalesModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, SalesMenu>();
        services.AddSingleton<IErpEndpoints, SalesEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, SalesQuotationManifest>();
        services.AddSingleton<IFeatureManifestProvider, SalesOrderManifest>();
        services.AddSingleton<IFeatureManifestProvider, SalesDeliveryManifest>();
        services.AddSingleton<IFeatureManifestProvider, SalesReturnManifest>();
        services.AddSingleton<IFeatureManifestProvider, SalesCollectionManifest>();
        services.AddSingleton<ILookupProvider, SalesLookups>();
        services.AddSingleton<IApprovalSource, QuotationApprovalSource>();
        services.AddSingleton<IApprovalSource, SalesOrderApprovalSource>();
        services.AddSingleton<IApprovalSource, SalesDeliveryApprovalSource>();
        services.AddSingleton<IApprovalSource, SalesReturnApprovalSource>();
        services.AddSingleton<IApprovalSource, SalesCollectionApprovalSource>();
        return services;
    }
}

public sealed class SalesEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/sales").WithTags("Sales");

        SalesQuotationEndpoints.Map(group);
        SalesOrderEndpoints.Map(group);
        SalesDeliveryEndpoints.Map(group);
        SalesReturnEndpoints.Map(group);
        SalesCollectionEndpoints.Map(group);
        SalesReportEndpoints.Map(group);
        CollectionReportEndpoints.Map(group);
        CustomerDueEndpoints.Map(group);
        ChargeAccounts.Map(group);
        SalesPrint.Map(group);
    }
}
