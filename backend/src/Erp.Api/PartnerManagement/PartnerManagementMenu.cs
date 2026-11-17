using Erp.Core.Modules;

namespace Erp.Api.PartnerManagement;

public sealed class PartnerManagementMenu : IErpMenuProvider
{
    public const string Module = "system-setup";

    public const string Customer = "system-setup.customer-profile";
    public const string Supplier = "system-setup.supplier-profile";

    private static readonly string[] Actions = [FeatureActions.View, FeatureActions.Create, FeatureActions.Edit, FeatureActions.Export];

    public IEnumerable<MenuModule> GetModules() =>
    [
        new(Module, new("System Setup", "সিস্টেম সেটআপ"), "setting", 10,
        [
            new("partner-management", new("Partner Management", "পার্টনার ব্যবস্থাপনা"), "shop", 30,
            [
                new("customer-profile", new("Customer Profile", "গ্রাহক প্রোফাইল"), 10, Actions),
                new("supplier-profile", new("Supplier Profile", "সরবরাহকারী প্রোফাইল"), 20, Actions),
            ]),
        ]),
    ];
}
