using Erp.Core.Modules;
using Erp.Modules.Inventory;

namespace Erp.Tests.Unit;

public sealed class ErpMenuCatalogTests
{
    private sealed class Provider(params MenuModule[] modules) : IErpMenuProvider
    {
        public IEnumerable<MenuModule> GetModules() => modules;
    }

    private static MenuModule Module(string key, params string[] features) =>
        new(key, new(key, key), "setting", 1,
            [new("sub", new("Sub", "Sub"), null, 1, features.Select((f, i) => new MenuFeature(f, new(f, f), i)).ToList())]);

    [Fact]
    public void Item_management_declares_six_features_under_system_setup()
    {
        var catalog = new ErpMenuCatalog([new ItemManagementMenu()]);

        Assert.Equal("system-setup", Assert.Single(catalog.Modules).Key);
        Assert.Equal(
            [
                ItemManagementMenu.ItemProfile, ItemManagementMenu.ItemDepartment, ItemManagementMenu.ItemCategory,
                ItemManagementMenu.ItemSubCategory, ItemManagementMenu.ItemMinorCategory, ItemManagementMenu.Uom,
            ],
            catalog.Features.Select(f => f.Code));
    }

    [Fact]
    public void Every_feature_contributes_one_permission_per_action()
    {
        var catalog = new ErpMenuCatalog([new ItemManagementMenu()]);

        var codes = new MenuPermissionProvider(catalog).GetPermissions().Select(p => p.Code.Value).ToList();

        Assert.Equal(36, codes.Count);
        Assert.Contains("system-setup.item-profile.view", codes);
        Assert.Contains("system-setup.uom.import", codes);
    }

    [Fact]
    public void A_feature_can_declare_its_own_actions()
    {
        var module = new MenuModule("reports", new("Reports", "রিপোর্ট"), "bar-chart", 1,
            [new("sales", new("Sales", "বিক্রয়"), null, 1, [new MenuFeature("daily-sales", new("Daily sales", "দৈনিক বিক্রয়"), 1, ["view", "export"])])]);

        var codes = new MenuPermissionProvider(new ErpMenuCatalog([new Provider(module)])).GetPermissions().Select(p => p.Code.Value);

        Assert.Equal(["reports.daily-sales.view", "reports.daily-sales.export"], codes);
    }

    [Fact]
    public void Modules_with_the_same_key_from_two_providers_are_merged()
    {
        var items = new MenuModule("system-setup", new("Setup", "Setup"), "setting", 1,
            [new("items", new("Items", "Items"), null, 1, [new MenuFeature("item", new("Item", "Item"), 1)])]);
        var users = new MenuModule("system-setup", new("Setup", "Setup"), "setting", 1,
            [new("users", new("Users", "Users"), null, 2, [new MenuFeature("user", new("User", "User"), 1)])]);
        var catalog = new ErpMenuCatalog([new Provider(items), new Provider(users)]);

        var module = Assert.Single(catalog.Modules);
        Assert.Equal(["system-setup.item", "system-setup.user"], catalog.Features.Select(f => f.Code));
        Assert.Equal(2, module.SubModules.Count);
    }

    [Fact]
    public void Rejects_duplicate_feature_codes()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            new ErpMenuCatalog([new Provider(Module("inventory", "item", "item"))]));

        Assert.Contains("inventory.item", error.Message);
    }

    [Fact]
    public void Rejects_keys_that_cannot_form_permission_codes()
    {
        Assert.Throws<InvalidOperationException>(() => new ErpMenuCatalog([new Provider(Module("System Setup", "item"))]));
    }
}
