using Erp.Api.Accounting.AccountingReports;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Api.Accounting.Journals;
using Erp.Api.Accounting.PeriodClosings;
using Erp.Api.Accounting.TaxDeposits;
using Erp.Api.Approvals;
using Erp.Api.Procurement.PurchasePayments;
using Erp.Api.Sales.SalesCollections;
using Erp.Api.Sales.SalesDeliveries;
using Erp.Api.Sales.SalesOrders;
using Erp.Api.Sales.SalesReturns;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.Accounting;

public static class AccountingModule
{
    public static readonly string[] Readers =
        [
            $"{AccountingMenu.ChartOfAccounts}.view",
            $"{AccountingMenu.PostingAccounts}.view",
            $"{AccountingMenu.BankAccounts}.view",
            $"{AccountingMenu.AccountLedger}.view",
            .. JournalEndpoints.Editors,
        ];

    public static readonly string[] SupplierReaders = [$"{AccountingMenu.SupplierLedger}.view", .. JournalEndpoints.Editors];

    public static readonly string[] BankReaders =
        
        [
            $"{AccountingMenu.BankAccounts}.view",
            .. PurchasePaymentEndpoints.Editors,
            .. TaxDepositEndpoints.Editors,
            .. SalesOrderEndpoints.Editors,
            .. SalesDeliveryEndpoints.Editors,
            .. SalesReturnEndpoints.Editors,
            .. SalesCollectionEndpoints.Editors,
        ];

    public static IServiceCollection AddAccountingModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, AccountingMenu>();
        services.AddSingleton<IErpEndpoints, AccountingEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, ChartOfAccountManifest>();
        services.AddSingleton<IFeatureManifestProvider, BankAccountManifest>();
        services.AddSingleton<IFeatureManifestProvider, JournalManifest>();
        services.AddSingleton<IFeatureManifestProvider, TaxDepositManifest>();
        services.AddSingleton<IApprovalSource, JournalApprovalSource>();
        services.AddSingleton<IApprovalSource, TaxDepositApprovalSource>();
        return services;
    }
}

public sealed class AccountingEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/accounting").WithTags("Accounting");
        ChartOfAccountEndpoints.Map(group);
        BankAccountEndpoints.Map(group);
        JournalEndpoints.Map(group);
        TaxDepositEndpoints.Map(group);
        PeriodClosingEndpoints.Map(group);
        AccountingReportEndpoints.Map(group);
    }
}
