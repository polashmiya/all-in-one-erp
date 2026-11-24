using Erp.Api.Accounting.AccountingReports;
using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Api.Accounting.Journals;
using Erp.Api.Approvals;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.Accounting;

public static class AccountingModule
{
    public static readonly string[] Readers =
        [
            $"{AccountingMenu.ChartOfAccounts}.view",
            $"{AccountingMenu.PostingAccounts}.view",
            $"{AccountingMenu.AccountLedger}.view",
            .. JournalEndpoints.Editors,
        ];

    public static readonly string[] SupplierReaders = [$"{AccountingMenu.SupplierLedger}.view", .. JournalEndpoints.Editors];

    public static IServiceCollection AddAccountingModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, AccountingMenu>();
        services.AddSingleton<IErpEndpoints, AccountingEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, ChartOfAccountManifest>();
        services.AddSingleton<IFeatureManifestProvider, JournalManifest>();
        services.AddSingleton<IApprovalSource, JournalApprovalSource>();
        return services;
    }
}

public sealed class AccountingEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/accounting").WithTags("Accounting");
        ChartOfAccountEndpoints.Map(group);
        JournalEndpoints.Map(group);
        AccountingReportEndpoints.Map(group);
    }
}
