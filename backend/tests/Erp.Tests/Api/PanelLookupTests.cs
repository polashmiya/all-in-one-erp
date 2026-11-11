using System.Net;
using System.Net.Http.Json;
using Erp.Api.Lookups;
using Erp.Api.Panel.Configuration;
using Erp.Api.Panel.Lookups;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Erp.Modules.Inventory;
using Erp.Modules.Inventory.ItemManagement.Items;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PanelLookupTests(ApiFactory factory)
{
    private async Task<HttpClient> PanelClientAsync()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);
        return factory.CreateClient(login.AccessToken);
    }

    private async Task<HttpClient> ErpClientAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private async Task<Guid> DemoAccountIdAsync()
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.BusinessUnits.AcrossTenants().Where(b => b.Id == factory.DhakaId).Select(b => b.AccountId).SingleAsync();
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<IEnumerable<string>> ErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys;
    }

    private static async Task RestoreAsync(Guid accountId)
    {
        await using var db = ApiFactory.CreateDbContext();
        await db.LookupValues.AcrossTenants().Where(v => v.AccountId == accountId && v.Code!.StartsWith("T-")).ExecuteDeleteAsync();
        await db.LookupValues.AcrossTenants()
            .Where(v => v.AccountId == accountId && v.LookupType!.Key == ItemLookups.SupplyType)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsDefault, v => v.Code == "STD"));
    }

    [Fact]
    public async Task Every_account_gets_the_default_lists()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();

        var types = await client.GetFromJsonAsync<List<LookupTypeDto>>("/api/panel/lookup-types");
        var itemTypes = await client.GetFromJsonAsync<List<LookupValueRow>>($"/api/panel/accounts/{accountId}/lookups/{ItemLookups.ItemType}");

        Assert.Equal([ItemLookups.ItemType, ItemLookups.SupplyType], types!.Select(t => t.Key).Order());
        Assert.Equal(8, itemTypes!.Count);
        Assert.Equal(("FG", "Finished Goods", "তৈরি পণ্য"), (itemTypes[0].Code, itemTypes[0].NameEn, itemTypes[0].NameBn));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/panel/accounts/{accountId}/lookups/unknown")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/panel/accounts/{Guid.NewGuid()}/lookups/{ItemLookups.ItemType}")).StatusCode);
    }

    [Fact]
    public async Task Values_can_be_added_changed_and_made_the_default()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        var list = $"/api/panel/accounts/{accountId}/lookups/{ItemLookups.SupplyType}";

        try
        {
            var created = await ReadAsync<LookupValueRow>(await client.PostAsJsonAsync(list,
                new SaveLookupValueRequest(" T-LUX ", " Luxury VAT ", "বিলাসী ভ্যাট", 70, IsDefault: false, IsActive: true)));
            Assert.Equal(("T-LUX", "Luxury VAT"), (created.Code, created.NameEn));

            Assert.Equal(["code"], await ErrorsAsync(await client.PostAsJsonAsync(list,
                new SaveLookupValueRequest("T-LUX", "Other", null, 80, false, true))));
            Assert.Equal(["isDefault", "nameEn"], (await ErrorsAsync(await client.PostAsJsonAsync(list,
                new SaveLookupValueRequest("T-BAD", "", null, 80, IsDefault: true, IsActive: false)))).Order());

            var updated = await ReadAsync<LookupValueRow>(await client.PutAsJsonAsync($"{list}/{created.Id}",
                new SaveLookupValueRequest("T-LUX", "Luxury VAT", "বিলাসী ভ্যাট", 5, IsDefault: true, IsActive: true)));
            Assert.True(updated.IsDefault);

            var values = await client.GetFromJsonAsync<List<LookupValueRow>>(list);
            Assert.Equal(created.Id, Assert.Single(values!, v => v.IsDefault).Id);
            Assert.Equal(created.Id, values![0].Id);

            var erp = await ErpClientAsync();
            var options = await erp.GetFromJsonAsync<List<LookupOption>>($"/api/erp/lookups/{ItemLookups.SupplyType}");
            Assert.Contains(options!, o => o.Id == created.Id && o.NameBn == "বিলাসী ভ্যাট" && o.IsDefault);

            var config = await erp.GetFromJsonAsync<FeatureConfig>($"/api/erp/features/{ItemManagementMenu.ItemProfile}/config");
            var salesSupply = config!.Tabs.SelectMany(t => t.Sections).SelectMany(s => s.Fields).Single(f => f.Key == "salesSupplyTypeId");
            Assert.Equal(created.Id.ToString(), salesSupply.DefaultValue);
        }
        finally
        {
            await RestoreAsync(accountId);
        }
    }

    [Fact]
    public async Task Screen_settings_accept_only_values_of_the_fields_list()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        var fields = $"/api/panel/accounts/{accountId}/configuration/{ItemManagementMenu.ItemProfile}/fields";
        var itemTypes = await client.GetFromJsonAsync<List<LookupValueRow>>($"/api/panel/accounts/{accountId}/lookups/{ItemLookups.ItemType}");
        var supplyTypes = await client.GetFromJsonAsync<List<LookupValueRow>>($"/api/panel/accounts/{accountId}/lookups/{ItemLookups.SupplyType}");
        await using var cleanup = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);

        Assert.Equal(["defaultValue"], await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/itemTypeId",
            new FieldSettingValues { DefaultValue = supplyTypes![0].Id.ToString() })));

        var hidden = await ReadAsync<FieldConfigRow>(await client.PutAsJsonAsync($"{fields}/itemTypeId",
            new FieldSettingValues { IsVisible = false, DefaultValue = itemTypes![0].Id.ToString() }));
        Assert.Equal((ItemLookups.ItemType, false), (hidden.Lookup, hidden.Override!.IsVisible));

        var supplyHidden = await client.PutAsJsonAsync($"{fields}/salesSupplyTypeId", new FieldSettingValues { IsVisible = false });
        Assert.Equal(HttpStatusCode.OK, supplyHidden.StatusCode);
    }
}
