using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.Procurement.PurchaseOrders;
using Erp.Api.Procurement.PurchasePayments;
using Erp.Api.Procurement.PurchaseReceives;
using Erp.Api.Procurement.PurchaseReports;
using Erp.Api.Procurement.PurchaseReturns;
using Erp.Api.Procurement.PurchaseRequisitions;
using Erp.Core.Authorization;
using Erp.Core.Modules;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;

namespace Erp.Api.Procurement;

public static class ProcurementModule
{
    public static readonly string[] Editors =
        [
            .. PurchaseRequisitionEndpoints.Editors,
            .. PurchaseOrderEndpoints.Editors,
            .. PurchaseReceiveEndpoints.Editors,
            .. PurchaseReturnEndpoints.Editors,
            .. PurchasePaymentEndpoints.Editors,
            $"{ProcurementMenu.PurchaseReport}.view",
        ];

    public static IServiceCollection AddProcurementModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, ProcurementMenu>();
        services.AddSingleton<IErpEndpoints, ProcurementEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, PurchaseRequisitionManifest>();
        services.AddSingleton<IFeatureManifestProvider, PurchaseOrderManifest>();
        services.AddSingleton<IFeatureManifestProvider, PurchaseReceiveManifest>();
        services.AddSingleton<IFeatureManifestProvider, PurchaseReturnManifest>();
        services.AddSingleton<IFeatureManifestProvider, PurchasePaymentManifest>();
        services.AddSingleton<ILookupProvider, ProcurementLookups>();
        services.AddSingleton<IApprovalSource, RequisitionApprovalSource>();
        services.AddSingleton<IApprovalSource, OrderApprovalSource>();
        services.AddSingleton<IApprovalSource, ReceiveApprovalSource>();
        services.AddSingleton<IApprovalSource, ReturnApprovalSource>();
        services.AddSingleton<IApprovalSource, PaymentApprovalSource>();
        return services;
    }
}

public sealed class ProcurementEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/procurement").WithTags("Procurement");

        PurchaseRequisitionEndpoints.Map(group);
        PurchaseOrderEndpoints.Map(group);
        PurchaseReceiveEndpoints.Map(group);
        PurchaseReturnEndpoints.Map(group);
        PurchasePaymentEndpoints.Map(group);
        PurchaseReportEndpoints.Map(group);
    }
}
