using System.Net;
using System.Net.Http.Json;
using Erp.Api.Panel.Configuration;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Erp.Modules.Inventory;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PanelFeatureConfigurationTests(ApiFactory factory)
{
    private async Task<HttpClient> PanelClientAsync()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);
        return factory.CreateClient(login.AccessToken);
    }

    private async Task<Guid> DemoAccountIdAsync()
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.BusinessUnits.AcrossTenants().Where(b => b.Id == factory.DhakaId).Select(b => b.AccountId).SingleAsync();
    }

    private static string Path(Guid accountId, string feature = ItemManagementMenu.ItemProfile) =>
        $"/api/panel/accounts/{accountId}/configuration/{feature}";

    private static string Unit(Guid? businessUnitId) => businessUnitId is null ? "" : $"?businessUnitId={businessUnitId}";

    private async Task<IEnumerable<string>> ErpFieldKeysAsync(Guid unit)
    {
        var login = await factory.ErpAdminLoginAsync();
        var config = await factory.CreateClient(login.AccessToken, unit)
            .GetFromJsonAsync<FeatureConfig>($"/api/erp/features/{ItemManagementMenu.ItemProfile}/config");
        return config!.Tabs.SelectMany(t => t.Sections).SelectMany(s => s.Fields).Select(f => f.Key);
    }

    private static async Task<IEnumerable<string>> ErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys;
    }

    [Fact]
    public async Task Editor_lists_every_field_with_its_defaults()
    {
        var client = await PanelClientAsync();

        var editor = await client.GetFromJsonAsync<FeatureConfigEditor>(Path(await DemoAccountIdAsync()));

        Assert.Equal(34, editor!.Fields.Count);
        Assert.Equal(("basic", "general"), (editor.Sections[0].Key, editor.Sections[0].TabKey));
        var name = editor.Fields[0];
        Assert.Equal(("name", true, true, 200, true), (name.Key, name.IsLocked, name.Base.IsRequired, name.MaxLengthLimit, name.CanList));
        Assert.Null(name.Override);
        var hsCode = editor.Fields.Single(f => f.Key == "hsCode");
        Assert.Equal((false, false, true, "basic"), (hsCode.CanList, hsCode.CanFilter, hsCode.Base.IsVisible, hsCode.Base.SectionKey));
    }

    [Fact]
    public async Task Business_unit_settings_override_the_account_for_that_unit_only()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        await using var cleanup = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);

        var hidden = await client.PutAsJsonAsync($"{Path(accountId)}/fields/hsCode", new FieldSettingValues { IsVisible = false, IsRequired = false });
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
        Assert.DoesNotContain("hsCode", await ErpFieldKeysAsync(factory.DhakaId));
        Assert.DoesNotContain("hsCode", await ErpFieldKeysAsync(factory.ChattogramId));

        var shown = await client.PutAsJsonAsync($"{Path(accountId)}/fields/hsCode{Unit(factory.DhakaId)}", new FieldSettingValues { IsVisible = true });
        var row = (await shown.Content.ReadFromJsonAsync<FieldConfigRow>())!;
        Assert.Equal((false, true), (row.Base.IsVisible, row.Override!.IsVisible));
        Assert.Contains("hsCode", await ErpFieldKeysAsync(factory.DhakaId));
        Assert.DoesNotContain("hsCode", await ErpFieldKeysAsync(factory.ChattogramId));

        var reset = await client.DeleteAsync($"{Path(accountId)}/fields/hsCode{Unit(factory.DhakaId)}");
        Assert.Null((await reset.Content.ReadFromJsonAsync<FieldConfigRow>())!.Override);
        Assert.DoesNotContain("hsCode", await ErpFieldKeysAsync(factory.DhakaId));
    }

    [Fact]
    public async Task Invalid_settings_are_rejected()
    {
        var client = await PanelClientAsync();
        var fields = $"{Path(await DemoAccountIdAsync())}/fields";
        await using var cleanup = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);

        Assert.Equal(["isRequired", "isVisible"],
            (await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/name", new FieldSettingValues { IsVisible = false, IsRequired = false }))).Order());
        Assert.Equal(["isVisible"],
            await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/itemTypeId", new FieldSettingValues { IsVisible = false })));
        Assert.Equal(["inList", "sectionKey"],
            (await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/hsCode", new FieldSettingValues { InList = true, SectionKey = "general" }))).Order());
        Assert.Equal(["maxLength"],
            await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/name", new FieldSettingValues { MaxLength = 500 })));
        Assert.Equal(["defaultValue", "maxValue"],
            (await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/salesVatPercentage", new FieldSettingValues { MaxValue = 150m, DefaultValue = "abc" }))).Order());
        Assert.Equal(["colSpan"],
            await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/code", new FieldSettingValues { ColSpan = 5 })));
    }

    [Fact]
    public async Task Unknown_screens_fields_and_units_are_not_found()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Path(accountId, ItemManagementMenu.Uom))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Path(Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Path(accountId)}{Unit(factory.OtherAccountUnitId)}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"{Path(accountId)}/fields/nope", new FieldSettingValues { IsVisible = false })).StatusCode);
    }
}
