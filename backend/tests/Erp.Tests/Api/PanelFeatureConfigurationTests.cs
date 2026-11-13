using System.Net;
using System.Net.Http.Json;
using Erp.Api.Panel.Configuration;
using Erp.Core.Domain.Metadata;
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
    public async Task Custom_fields_are_added_checked_and_deactivated()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        var fields = $"{Path(accountId)}/fields";
        await using var cleanup = await CustomFieldScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);

        var created = await client.PostAsJsonAsync(fields, new CreateCustomFieldRequest(" Warranty months ", "ওয়ারেন্টি", FieldTypes.Number, "basic", null));
        var row = (await created.Content.ReadFromJsonAsync<FieldConfigRow>())!;
        Assert.Equal(("warrantyMonths", true, true, false, "basic"), (row.Key, row.IsCustom, row.IsActive, row.CanList, row.Base.SectionKey));
        Assert.Contains("warrantyMonths", await ErpFieldKeysAsync(factory.DhakaId));

        Assert.Equal(["labelEn"],
            await ErrorsAsync(await client.PostAsJsonAsync(fields, new CreateCustomFieldRequest("Warranty Months", null, FieldTypes.Text, "basic", null))));
        Assert.Equal(["labelEn"],
            await ErrorsAsync(await client.PostAsJsonAsync(fields, new CreateCustomFieldRequest("Name", null, FieldTypes.Text, "basic", null))));
        Assert.Equal(["lookup", "sectionKey"],
            (await ErrorsAsync(await client.PostAsJsonAsync(fields, new CreateCustomFieldRequest("Grade", null, FieldTypes.Lookup, "nope", "no-list")))).Order());
        Assert.Equal(["type"],
            await ErrorsAsync(await client.PostAsJsonAsync(fields, new CreateCustomFieldRequest("Photo", null, FieldTypes.Image, "basic", null))));
        Assert.Equal(["inList"],
            await ErrorsAsync(await client.PutAsJsonAsync($"{fields}/warrantyMonths", new FieldSettingValues { InList = true })));

        var off = await client.PutAsJsonAsync($"{fields}/warrantyMonths/status", new CustomFieldStatusRequest(false));
        Assert.False((await off.Content.ReadFromJsonAsync<FieldConfigRow>())!.IsActive);
        Assert.DoesNotContain("warrantyMonths", await ErpFieldKeysAsync(factory.DhakaId));

        var editor = await client.GetFromJsonAsync<FeatureConfigEditor>(Path(accountId));
        Assert.False(editor!.Fields.Single(f => f.Key == "warrantyMonths").IsActive);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"{fields}/name/status", new CustomFieldStatusRequest(false))).StatusCode);
    }

    [Fact]
    public async Task Sections_are_renamed_resized_hidden_and_reset()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        var sections = $"{Path(accountId)}/sections";
        await using var cleanup = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);

        var editor = (await client.GetFromJsonAsync<FeatureConfigEditor>(Path(accountId)))!;
        var optional = editor.Layout.First(s => !s.IsTab &&
            editor.Fields.Where(f => f.Base.SectionKey == s.Key).All(f => !f.IsLocked && f.Base.IsRequired != true));

        var saved = await client.PutAsJsonAsync($"{sections}/{optional.Key}{Unit(factory.DhakaId)}",
            new SectionSettingValues { LabelEn = " Extra ", Columns = 2, IsVisible = false });
        var row = (await saved.Content.ReadFromJsonAsync<SectionConfigRow>())!;
        Assert.Equal(("Extra", 2, false, true), (row.Override!.LabelEn, row.Override.Columns, row.Override.IsVisible, row.Base.IsVisible));

        var dhaka = (await client.GetFromJsonAsync<FeatureConfig>($"{Path(accountId)}/preview{Unit(factory.DhakaId)}"))!;
        var chattogram = (await client.GetFromJsonAsync<FeatureConfig>($"{Path(accountId)}/preview{Unit(factory.ChattogramId)}"))!;
        Assert.DoesNotContain(optional.Key, dhaka.Tabs.SelectMany(t => t.Sections).Select(s => s.Key));
        Assert.Contains(optional.Key, chattogram.Tabs.SelectMany(t => t.Sections).Select(s => s.Key));

        Assert.Equal(["isVisible"],
            await ErrorsAsync(await client.PutAsJsonAsync($"{sections}/basic", new SectionSettingValues { IsVisible = false })));
        Assert.Equal(["tabKey"],
            await ErrorsAsync(await client.PutAsJsonAsync($"{sections}/basic", new SectionSettingValues { TabKey = "nope" })));
        Assert.Equal(["columns"],
            await ErrorsAsync(await client.PutAsJsonAsync($"{sections}/basic", new SectionSettingValues { Columns = 5 })));

        var reset = await client.DeleteAsync($"{sections}/{optional.Key}{Unit(factory.DhakaId)}");
        var back = (await reset.Content.ReadFromJsonAsync<SectionConfigRow>())!;
        Assert.Equal((back.Base.LabelEn, back.Base.Columns, back.Base.IsVisible), (back.Override!.LabelEn, back.Override.Columns, back.Override.IsVisible));

        await client.DeleteAsync($"{Path(accountId)}/layout{Unit(factory.DhakaId)}");
        var unit = (await client.GetFromJsonAsync<FeatureConfigEditor>($"{Path(accountId)}{Unit(factory.DhakaId)}"))!;
        Assert.All(unit.Layout, s => Assert.Null(s.Override));
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"{sections}/nope", new SectionSettingValues { Columns = 2 })).StatusCode);
    }

    [Fact]
    public async Task Layout_arranges_tabs_sections_and_fields_and_stores_only_what_changed()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        await using var cleanup = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);

        var before = (await client.GetFromJsonAsync<FeatureConfig>($"{Path(accountId)}/preview"))!;
        LayoutRequest Layout(FeatureConfig config) => new(config.Tabs
            .Select(t => new LayoutTab(t.Key, t.Sections.Select(s => new LayoutSection(s.Key, s.Fields.Select(f => f.Key).ToList())).ToList()))
            .ToList());

        var same = await client.PutAsJsonAsync($"{Path(accountId)}/layout", Layout(before));
        var unchanged = (await same.Content.ReadFromJsonAsync<FeatureConfigEditor>())!;
        Assert.All(unchanged.Fields, f => Assert.Null(f.Override));
        Assert.All(unchanged.Layout, s => Assert.Null(s.Override));

        var layout = Layout(before);
        var first = layout.Tabs[0].Sections[0];
        var second = layout.Tabs[0].Sections[1];
        var moved = second.Fields[0];
        var arranged = new LayoutRequest([
            new LayoutTab(layout.Tabs[0].Key, [
                new LayoutSection(second.Key, second.Fields.Skip(1).ToList()),
                new LayoutSection(first.Key, [moved, .. first.Fields]),
                .. layout.Tabs[0].Sections.Skip(2),
            ]),
            .. layout.Tabs.Skip(1),
        ]);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{Path(accountId)}/layout", arranged)).StatusCode);
        var after = (await client.GetFromJsonAsync<FeatureConfig>($"{Path(accountId)}/preview"))!;
        Assert.Equal([second.Key, first.Key], after.Tabs[0].Sections.Take(2).Select(s => s.Key));
        Assert.Equal(moved, after.Tabs[0].Sections[1].Fields[0].Key);

        var unknown = new LayoutRequest([new LayoutTab(layout.Tabs[0].Key, [new LayoutSection(first.Key, ["nope"])])]);
        Assert.Equal(["tabs"], await ErrorsAsync(await client.PutAsJsonAsync($"{Path(accountId)}/layout", unknown)));
    }

    [Fact]
    public async Task A_business_unit_layout_is_its_own_until_it_goes_back_to_the_account_layout()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        await using var cleanup = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);
        async Task<FeatureConfig> PreviewAsync(Guid? unit) =>
            (await client.GetFromJsonAsync<FeatureConfig>($"{Path(accountId)}/preview{Unit(unit)}"))!;
        async Task<FeatureConfigEditor> EditorAsync(Guid? unit) =>
            (await client.GetFromJsonAsync<FeatureConfigEditor>($"{Path(accountId)}{Unit(unit)}"))!;
        static List<string> Sections(FeatureConfig config) => config.Tabs[0].Sections.Select(s => s.Key).ToList();
        static List<string> Fields(FeatureConfig config) => config.Tabs[0].Sections[0].Fields.Select(f => f.Key).ToList();
        static LayoutRequest Layout(FeatureConfig config, Func<List<LayoutSection>, List<LayoutSection>> arrange) => new(config.Tabs
            .Select((t, i) =>
            {
                var sections = t.Sections.Select(s => new LayoutSection(s.Key, s.Fields.Select(f => f.Key).ToList())).ToList();
                return new LayoutTab(t.Key, i == 0 ? arrange(sections) : sections);
            })
            .ToList());

        var standard = await PreviewAsync(null);
        Assert.False((await EditorAsync(factory.DhakaId)).OwnLayout);

        var swapped = Layout(standard, s => [s[1], s[0], .. s.Skip(2)]);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{Path(accountId)}/layout{Unit(factory.DhakaId)}", swapped)).StatusCode);
        var dhaka = await PreviewAsync(factory.DhakaId);
        Assert.Equal([Sections(standard)[1], Sections(standard)[0]], Sections(dhaka).Take(2));
        Assert.Equal(Sections(standard), Sections(await PreviewAsync(factory.ChattogramId)));
        Assert.Equal(Sections(standard), Sections(await PreviewAsync(null)));

        var account = await EditorAsync(null);
        Assert.Equal([factory.DhakaId], account.LayoutUnitIds);
        Assert.False(account.OwnLayout);
        Assert.True((await EditorAsync(factory.DhakaId)).OwnLayout);

        var reversed = Layout(standard, s => [s[0] with { Fields = s[0].Fields.Reverse().ToList() }, .. s.Skip(1)]);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{Path(accountId)}/layout", reversed)).StatusCode);
        Assert.True((await EditorAsync(null)).OwnLayout);
        Assert.Equal(Fields(standard).AsEnumerable().Reverse(), Fields(await PreviewAsync(factory.ChattogramId)));
        Assert.Equal(Fields(dhaka), Fields(await PreviewAsync(factory.DhakaId)));

        var reset = await client.DeleteAsync($"{Path(accountId)}/layout{Unit(factory.DhakaId)}");
        var editor = (await reset.Content.ReadFromJsonAsync<FeatureConfigEditor>())!;
        Assert.Equal((false, 0), (editor.OwnLayout, editor.LayoutUnitIds.Count));
        Assert.Equal(Fields(await PreviewAsync(null)), Fields(await PreviewAsync(factory.DhakaId)));
        Assert.Equal(Sections(standard), Sections(await PreviewAsync(factory.DhakaId)));

        await client.DeleteAsync($"{Path(accountId)}/layout");
        Assert.False((await EditorAsync(null)).OwnLayout);
        Assert.Equal(Fields(standard), Fields(await PreviewAsync(factory.ChattogramId)));
    }

    [Fact]
    public async Task Every_change_is_kept_in_the_history_with_who_and_what()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();
        var history = $"/api/panel/accounts/{accountId}/history";
        await using var cleanup = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);
        await using var custom = await CustomFieldScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile);
        var label = $"Label {Guid.NewGuid():N}"[..20];

        await client.PutAsJsonAsync($"{Path(accountId)}/fields/hsCode{Unit(factory.DhakaId)}", new FieldSettingValues { LabelEn = label });
        await client.PutAsJsonAsync($"{Path(accountId)}/fields/hsCode{Unit(factory.DhakaId)}", new FieldSettingValues { LabelEn = label });
        await client.PostAsJsonAsync($"{Path(accountId)}/fields", new CreateCustomFieldRequest("History check", null, FieldTypes.Text, "basic", null));
        await client.PutAsJsonAsync($"{Path(accountId)}/fields/historyCheck/status", new CustomFieldStatusRequest(false));

        var fields = (await client.GetFromJsonAsync<ConfigurationChangeList>(
            $"{history}?feature={ItemManagementMenu.ItemProfile}&businessUnitId={factory.DhakaId}&area={ConfigurationAreas.Field}&pageSize=5"))!;
        var change = fields.Items[0];
        Assert.Equal(("hsCode", "Dhaka Depot"), (change.Key, change.BusinessUnitName));
        Assert.NotNull(change.UserName);
        Assert.Equal(label, change.After!.Value.GetProperty("labelEn").GetString());
        Assert.NotEqual(label, fields.Items.Skip(1).FirstOrDefault()?.After?.GetProperty("labelEn").GetString());

        var customChanges = (await client.GetFromJsonAsync<ConfigurationChangeList>(
            $"{history}?area={ConfigurationAreas.CustomField}&pageSize=2"))!;
        Assert.Equal(["historyCheck", "historyCheck"], customChanges.Items.Select(c => c.Key));
        Assert.Null(customChanges.Items[1].Before);
        Assert.False(customChanges.Items[0].After!.Value.GetProperty("isActive").GetBoolean());

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/panel/accounts/{Guid.NewGuid()}/history")).StatusCode);
    }

    [Fact]
    public async Task Unknown_screens_fields_and_units_are_not_found()
    {
        var client = await PanelClientAsync();
        var accountId = await DemoAccountIdAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Path(accountId, $"{ItemManagementMenu.Module}.nope"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Path(Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Path(accountId)}{Unit(factory.OtherAccountUnitId)}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"{Path(accountId)}/fields/nope", new FieldSettingValues { IsVisible = false })).StatusCode);
    }
}
