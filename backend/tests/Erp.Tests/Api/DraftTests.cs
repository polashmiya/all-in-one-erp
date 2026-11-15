using System.Net;
using System.Net.Http.Json;
using Erp.Api.Drafts;
using Erp.Modules.Inventory;
using Erp.Tests.Infrastructure;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class DraftTests(ApiFactory factory)
{
    private const string ErpDrafts = "/api/erp/drafts";
    private static readonly string Feature = ItemManagementMenu.ItemProfile;

    [Fact]
    public async Task A_draft_is_saved_listed_continued_and_deleted_in_its_unit_only()
    {
        var dhaka = await ErpAsync(factory.DhakaId);
        var chattogram = await ErpAsync(factory.ChattogramId);
        var title = $"Draft {Guid.NewGuid():N}";

        var created = await dhaka.PostAsJsonAsync(ErpDrafts, new SaveDraftRequest(Feature, title, """{"name":"Half"}"""));
        var draft = (await created.Content.ReadFromJsonAsync<DraftDetail>())!;
        await dhaka.PutAsJsonAsync($"{ErpDrafts}/{draft.Id}", new SaveDraftRequest(Feature, title, """{"name":"Done"}"""));

        var inDhaka = await dhaka.GetFromJsonAsync<List<DraftRow>>($"{ErpDrafts}?feature={Feature}");
        var inChattogram = await chattogram.GetFromJsonAsync<List<DraftRow>>($"{ErpDrafts}?feature={Feature}");
        var continued = await dhaka.GetFromJsonAsync<DraftDetail>($"{ErpDrafts}/{draft.Id}");
        var deleted = await dhaka.DeleteAsync($"{ErpDrafts}/{draft.Id}");

        Assert.Contains(inDhaka!, d => d.Id == draft.Id && d.Title == title && d.SavedBy != null);
        Assert.DoesNotContain(inChattogram!, d => d.Id == draft.Id);
        Assert.Equal("""{"name":"Done"}""", continued!.Data);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task A_draft_needs_a_screen_the_unit_has()
    {
        var response = await (await ErpAsync(factory.DhakaId))
            .PostAsJsonAsync(ErpDrafts, new SaveDraftRequest("nope.screen", "x", "{}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Panel_drafts_are_shared_by_panel_users()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);
        var client = factory.CreateClient(login.AccessToken);

        var created = await client.PostAsJsonAsync("/api/panel/drafts", new SaveDraftRequest("accounts", "New account", """{"a":1}"""));
        var list = await client.GetFromJsonAsync<List<DraftRow>>("/api/panel/drafts?feature=accounts");

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Contains(list!, d => d.Title == "New account");
    }

    private async Task<HttpClient> ErpAsync(Guid unitId)
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, unitId);
    }
}
