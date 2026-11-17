using System.Net;
using System.Net.Http.Json;
using Erp.Api.Navigation;
using Erp.Api.Panel.Accounts;
using Erp.Modules.Inventory;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Tests.Api;

/// <summary>Module → Sub-module → Feature menus: the Panel catalog, account grants and the ERP user's menu.</summary>
[Collection(ApiCollection.Name)]
public sealed class ErpMenuTests(ApiFactory factory)
{
    private async Task<HttpClient> PanelClientAsync()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);
        return factory.CreateClient(login.AccessToken);
    }

    private async Task<List<ErpMenuModule>> ErpMenuAsync(string email, string password, Guid? businessUnitId)
    {
        var login = await factory.ErpLoginAsync(email, password);
        return (await factory.CreateClient(login.AccessToken, businessUnitId).GetFromJsonAsync<List<ErpMenuModule>>("/api/erp/navigation"))!;
    }

    private static IEnumerable<string> FeatureCodes(IEnumerable<ErpMenuModule> menu) =>
        menu.SelectMany(m => m.SubModules).SelectMany(s => s.Features).Select(f => f.Code);

    [Fact]
    public async Task Panel_catalog_lists_system_setup_item_management_and_its_features_in_order()
    {
        var client = await PanelClientAsync();

        var catalog = await client.GetFromJsonAsync<List<ErpMenuModule>>("/api/panel/erp-menu");

        var module = Assert.Single(catalog!);
        Assert.Equal(("system-setup", "System Setup", "সিস্টেম সেটআপ"), (module.Key, module.Label.En, module.Label.Bn));
        Assert.Equal(["Item Management", "User Management", "Partner Management"], module.SubModules.Select(s => s.Label.En));
        var subModule = module.SubModules[0];
        Assert.Equal("Item Management", subModule.Label.En);
        Assert.Equal(
            ["Item Profile", "Item Department", "Item Category", "Item Sub Category", "Item Minor Category", "UOM"],
            subModule.Features.Select(f => f.Label.En));
        Assert.Equal("/app/system-setup/item-profile", subModule.Features[0].Path);
    }

    [Fact]
    public async Task Primary_admin_sees_every_feature_given_to_the_account()
    {
        var menu = await ErpMenuAsync(factory.Seed.ErpAdmin.Email, factory.Seed.ErpAdmin.Password, factory.DhakaId);

        Assert.Equal(11, FeatureCodes(menu).Count());
    }

    [Fact]
    public async Task Regular_user_sees_only_features_their_role_may_view()
    {
        var viewer = await ErpMenuAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword, factory.DhakaId);
        var newUser = await ErpMenuAsync(ApiFactory.NewUserEmail, ApiFactory.TestPassword, factory.DhakaId);

        Assert.Equal(11, FeatureCodes(viewer).Count());
        Assert.Empty(newUser);
    }

    [Fact]
    public async Task Regular_user_without_a_business_unit_sees_nothing()
    {
        Assert.Empty(await ErpMenuAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword, businessUnitId: null));
    }

    [Fact]
    public async Task Account_without_features_gets_no_menu_and_no_permissions_even_for_its_admin()
    {
        var login = await factory.ErpLoginAsync(ApiFactory.OtherAdminEmail, ApiFactory.TestPassword);
        var client = factory.CreateClient(login.AccessToken, factory.OtherAccountUnitId);

        Assert.Empty((await client.GetFromJsonAsync<List<ErpMenuModule>>("/api/erp/navigation"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/erp/__probe/view")).StatusCode);
    }

    [Fact]
    public async Task Panel_can_change_an_accounts_features_and_the_erp_follows()
    {
        var panel = await PanelClientAsync();
        var code = $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant();
        var email = $"menu.{code.ToLowerInvariant()}@test.example";
        var setup = await panel.PostAsJsonAsync("/api/panel/accounts/setup",
            PanelAccountTests.Request(code, email, "Menus@123", [ItemManagementMenu.ItemProfile, ItemManagementMenu.Uom]));
        var created = (await setup.Content.ReadFromJsonAsync<AccountSetupResult>())!;
        Assert.Equal(2, created.Features);

        var login = await factory.ErpReadyLoginAsync(email, "Menus@123");
        var erp = factory.CreateClient(login.AccessToken, login.BusinessUnits[0].Id);
        Assert.Equal([ItemManagementMenu.ItemProfile, ItemManagementMenu.Uom],
            FeatureCodes((await erp.GetFromJsonAsync<List<ErpMenuModule>>("/api/erp/navigation"))!));

        var update = await panel.PutAsJsonAsync($"/api/panel/accounts/{created.AccountId}/features",
            new AccountFeaturesRequest([ItemManagementMenu.ItemCategory, ItemManagementMenu.ItemSubCategory, ItemManagementMenu.Uom]));
        update.EnsureSuccessStatusCode();

        // Existing sessions see the change on the next request: no new token needed.
        Assert.Equal([ItemManagementMenu.ItemCategory, ItemManagementMenu.ItemSubCategory, ItemManagementMenu.Uom],
            FeatureCodes((await erp.GetFromJsonAsync<List<ErpMenuModule>>("/api/erp/navigation"))!));
        var granted = await panel.GetFromJsonAsync<AccountFeaturesResponse>($"/api/panel/accounts/{created.AccountId}/features");
        Assert.Equal(3, granted!.FeatureCodes.Count);
    }

    [Fact]
    public async Task Unknown_feature_codes_are_rejected()
    {
        var panel = await PanelClientAsync();
        var code = $"U{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        var setup = await panel.PostAsJsonAsync("/api/panel/accounts/setup",
            PanelAccountTests.Request(code, $"u.{code.ToLowerInvariant()}@test.example", features: ["system-setup.no-such-screen"]));
        var update = await panel.PutAsJsonAsync($"/api/panel/accounts/{Guid.NewGuid()}/features", new AccountFeaturesRequest([]));

        Assert.Equal(HttpStatusCode.BadRequest, setup.StatusCode);
        Assert.Contains("featureCodes", (await setup.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
    }

    [Fact]
    public async Task Erp_token_cannot_change_account_features()
    {
        var erp = await factory.ErpAdminLoginAsync();

        var response = await factory.CreateClient(erp.AccessToken)
            .PutAsJsonAsync($"/api/panel/accounts/{erp.User.AccountId}/features", new AccountFeaturesRequest([]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
