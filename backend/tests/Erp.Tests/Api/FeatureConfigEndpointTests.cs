using System.Net;
using System.Net.Http.Json;
using Erp.Api.Navigation;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Metadata;
using Erp.Modules.Inventory;
using Erp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class FeatureConfigEndpointTests(ApiFactory factory)
{
    private const string ItemProfileConfig = $"/api/erp/features/{ItemManagementMenu.ItemProfile}/config";

    private async Task<HttpClient> ClientAsync(string email, string password, Guid? unit)
    {
        var login = await factory.ErpLoginAsync(email, password);
        return factory.CreateClient(login.AccessToken, unit);
    }

    private Task<HttpClient> AdminAsync() => ClientAsync(factory.Seed.ErpAdmin.Email, factory.Seed.ErpAdmin.Password, factory.DhakaId);

    private static IEnumerable<ConfigField> Fields(FeatureConfig config) =>
        config.Tabs.SelectMany(t => t.Sections).SelectMany(s => s.Fields);

    private static async Task<int> ItemProfileVersionAsync(HttpClient client)
    {
        var menu = await client.GetFromJsonAsync<List<ErpMenuModule>>("/api/erp/navigation");
        return menu!.SelectMany(m => m.SubModules).SelectMany(s => s.Features)
            .Single(f => f.Code == ItemManagementMenu.ItemProfile).ConfigVersion;
    }

    [Fact]
    public async Task Item_profile_config_comes_from_the_manifest()
    {
        var client = await AdminAsync();

        var config = await client.GetFromJsonAsync<FeatureConfig>(ItemProfileConfig);

        Assert.Equal(ItemManagementMenu.ItemProfile, config!.Feature);
        Assert.Equal(await ItemProfileVersionAsync(client), config.Version);
        Assert.Equal(["general", "pricing", "tax", "status"], config.Tabs.Select(t => t.Key));
        Assert.Equal(["basic", "classification", "barcode"], config.Tabs[0].Sections.Select(s => s.Key));
        Assert.Equal(34, Fields(config).Count());

        var name = Fields(config).First();
        Assert.Equal(("name", "Item name", "আইটেমের নাম", true, 200), (name.Key, name.Label.En, name.Label.Bn, name.Required, name.MaxLength));
        var category = Fields(config).Single(f => f.Key == "categoryId");
        Assert.Equal((FieldTypes.Lookup, "item-category", "departmentId"), (category.Type, category.Lookup, category.ParentField));

        Assert.Equal(["name", "code", "categoryId", "itemTypeId", "uomId", "salesPrice"], config.Columns.Select(c => c.Key));
        Assert.Equal(["itemTypeId", "departmentId", "categoryId"], config.Filters);
    }

    [Fact]
    public async Task Account_settings_change_the_config_and_its_version()
    {
        var client = await AdminAsync();
        var before = await ItemProfileVersionAsync(client);

        await using var settings = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile,
            ("hsCode", s => s.IsVisible = false),
            ("supplierName", s => (s.IsRequired, s.LabelBn) = (true, "ঔষধ সরবরাহকারী")));

        var config = await client.GetFromJsonAsync<FeatureConfig>(ItemProfileConfig);

        Assert.Equal(before + 1, config!.Version);
        Assert.Equal(before + 1, await ItemProfileVersionAsync(client));
        Assert.DoesNotContain(Fields(config), f => f.Key == "hsCode");
        var supplier = Fields(config).Single(f => f.Key == "supplierName");
        Assert.Equal((true, "ঔষধ সরবরাহকারী"), (supplier.Required, supplier.Label.Bn));
    }

    [Fact]
    public async Task Users_need_the_view_permission_and_every_item_screen_has_a_manifest()
    {
        var viewer = await ClientAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword, factory.DhakaId);
        var newUser = await ClientAsync(ApiFactory.NewUserEmail, ApiFactory.TestPassword, factory.DhakaId);
        var otherAdmin = await ClientAsync(ApiFactory.OtherAdminEmail, ApiFactory.TestPassword, factory.OtherAccountUnitId);
        var admin = await AdminAsync();

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync(ItemProfileConfig)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await newUser.GetAsync(ItemProfileConfig)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherAdmin.GetAsync(ItemProfileConfig)).StatusCode);
        foreach (var feature in new[] { ItemManagementMenu.ItemDepartment, ItemManagementMenu.ItemCategory, ItemManagementMenu.ItemSubCategory,
                     ItemManagementMenu.ItemMinorCategory, ItemManagementMenu.Uom })
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/erp/features/{feature}/config")).StatusCode);
    }

    [Fact]
    public async Task Startup_sync_is_idempotent()
    {
        await using var db = ApiFactory.CreateDbContext();
        var before = await db.FeatureFields.CountAsync();
        var versions = await db.AccountFeatures.AcrossTenants().OrderBy(af => af.Id).Select(af => af.ConfigVersion).ToListAsync();

        await FeatureManifestSynchronizer.SyncAsync(
            db,
            factory.Services.GetRequiredService<FeatureManifestCatalog>(),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(before, await db.FeatureFields.CountAsync());
        Assert.Equal(versions, await db.AccountFeatures.AcrossTenants().OrderBy(af => af.Id).Select(af => af.ConfigVersion).ToListAsync());
    }
}
