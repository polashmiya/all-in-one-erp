using System.Net;
using System.Net.Http.Json;
using Erp.Api.Auth;
using Erp.Api.Navigation;
using Erp.Api.Panel.ErpUsers;
using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Modules.Inventory;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PanelErpUserTests(ApiFactory factory)
{
    [Fact]
    public async Task The_list_shows_users_of_every_account_with_their_units()
    {
        var panel = await SuperAdminAsync();
        var (id, email) = await CreateUserAsync();

        var list = await panel.GetFromJsonAsync<ErpUserListResponse>($"/api/panel/erp-users?search={email}");
        var admins = await panel.GetFromJsonAsync<ErpUserListResponse>("/api/panel/erp-users?type=admins&pageSize=100");

        var user = Assert.Single(list!.Items);
        Assert.Equal(id, user.Id);
        Assert.Equal("Demo Pharma Ltd", user.AccountName);
        Assert.Equal(DemoDataSeeder.ViewerRole, Assert.Single(user.Units).RoleName);
        Assert.Contains(admins!.Items, u => u.Email == factory.Seed.ErpAdmin.Email && u.IsAccountAdmin);
        Assert.All(admins.Items, u => Assert.True(u.IsAccountAdmin));
    }

    [Fact]
    public async Task Implementers_can_read_but_not_change_users()
    {
        var implementer = await ImplementerAsync();
        var (id, _) = await CreateUserAsync();

        Assert.Equal(HttpStatusCode.OK, (await implementer.GetAsync($"/api/panel/erp-users/{id}")).StatusCode);
        var change = await implementer.PutAsJsonAsync($"/api/panel/erp-users/{id}/status", new ErpUserStatusRequest(false));
        Assert.Equal(HttpStatusCode.Forbidden, change.StatusCode);
    }

    [Fact]
    public async Task Deactivating_a_user_ends_the_session_at_once()
    {
        var panel = await SuperAdminAsync();
        var (id, email) = await CreateUserAsync();
        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);

        var response = await panel.PutAsJsonAsync($"/api/panel/erp-users/{id}/status", new ErpUserStatusRequest(false));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient(login.AccessToken).GetAsync("/api/erp/auth/me")).StatusCode);
        var detail = await panel.GetFromJsonAsync<ErpUserDetail>($"/api/panel/erp-users/{id}");
        Assert.False(detail!.User.IsActive);
        Assert.NotEmpty(detail.History);
        Assert.NotEmpty(detail.Logins);
    }

    [Fact]
    public async Task The_last_active_admin_of_an_account_cannot_be_deactivated()
    {
        var panel = await SuperAdminAsync();
        var otherAdmin = await panel.GetFromJsonAsync<ErpUserListResponse>($"/api/panel/erp-users?search={ApiFactory.OtherAdminEmail}");

        var response = await panel.PutAsJsonAsync(
            $"/api/panel/erp-users/{otherAdmin!.Items.Single().Id}/status", new ErpUserStatusRequest(false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("isActive", (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task A_reset_password_is_shown_once_and_must_be_changed()
    {
        var panel = await SuperAdminAsync();
        var (id, email) = await CreateUserAsync();

        var response = await panel.PostAsJsonAsync($"/api/panel/erp-users/{id}/reset-password", new ResetErpUserPasswordRequest(null));
        var result = (await response.Content.ReadFromJsonAsync<ResetErpUserPasswordResult>())!;

        Assert.False(string.IsNullOrEmpty(result.GeneratedPassword));
        Assert.True((await factory.ErpLoginAsync(email, result.GeneratedPassword!)).MustChangePassword);
    }

    [Fact]
    public async Task Unlock_lets_a_locked_user_sign_in_again()
    {
        var panel = await SuperAdminAsync();
        var (id, email) = await CreateUserAsync();
        for (var i = 0; i < 3; i++) await LoginAsync(email, "Wrong@12345");
        Assert.True((await panel.GetFromJsonAsync<ErpUserDetail>($"/api/panel/erp-users/{id}"))!.User.IsLocked);

        await panel.PostAsync($"/api/panel/erp-users/{id}/unlock", null);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, ApiFactory.TestPassword)).StatusCode);
    }

    [Fact]
    public async Task Edit_rejects_a_username_that_is_taken()
    {
        var panel = await SuperAdminAsync();
        var (id, email) = await CreateUserAsync();

        var response = await panel.PutAsJsonAsync($"/api/panel/erp-users/{id}",
            new UpdateErpUserRequest("Renamed", "viewer.user", email, null));

        Assert.Contains("username", (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task An_added_admin_has_every_unit_and_must_change_the_password()
    {
        var panel = await SuperAdminAsync();
        var accountId = await DemoAccountIdAsync();
        var username = $"admin.{Guid.NewGuid():N}"[..20];

        var response = await panel.PostAsJsonAsync($"/api/panel/accounts/{accountId}/admins",
            new AddAccountAdminRequest("Second Admin", username, $"{username}@demopharma.com", null, "Admin@12345"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await factory.ErpLoginAsync(username, "Admin@12345");
        Assert.True(login.MustChangePassword);
        Assert.True(login.User.IsAccountAdmin);
        Assert.Equal(2, login.BusinessUnits.Count);
    }

    [Fact]
    public async Task A_feature_turned_off_for_a_unit_leaves_its_menu_and_permissions()
    {
        var panel = await SuperAdminAsync();
        var accountId = await DemoAccountIdAsync();
        var url = $"/api/panel/accounts/{accountId}/business-units/{factory.ChattogramId}/features";

        try
        {
            var save = await panel.PutAsJsonAsync(url, new UnitFeaturesDto([ItemManagementMenu.Uom]));
            Assert.Equal([ItemManagementMenu.Uom], (await save.Content.ReadFromJsonAsync<UnitFeaturesDto>())!.DisabledFeatureCodes);

            var admin = await factory.ErpAdminLoginAsync();
            var inChattogram = await Menu(factory.CreateClient(admin.AccessToken, factory.ChattogramId));
            var inDhaka = await Menu(factory.CreateClient(admin.AccessToken, factory.DhakaId));

            Assert.DoesNotContain(ItemManagementMenu.Uom, inChattogram);
            Assert.Contains(ItemManagementMenu.Uom, inDhaka);
        }
        finally
        {
            await panel.PutAsJsonAsync(url, new UnitFeaturesDto([]));
        }
    }

    private static async Task<List<string>> Menu(HttpClient client) =>
        (await client.GetFromJsonAsync<List<ErpMenuModule>>("/api/erp/navigation"))!
            .SelectMany(m => m.SubModules.SelectMany(s => s.Features.Select(f => f.Code)))
            .ToList();

    private async Task<HttpClient> SuperAdminAsync()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);
        return factory.CreateClient(login.AccessToken);
    }

    private async Task<HttpClient> ImplementerAsync()
    {
        var email = $"implementer.{Guid.NewGuid():N}@myerp.local";
        var hasher = factory.Services.GetRequiredService<IPasswordHasher>();
        await using (var db = ApiFactory.CreateDbContext())
        {
            db.PanelUsers.Add(new PanelUser
            {
                Email = email,
                FullName = "Implementer",
                PasswordHash = hasher.Hash(ApiFactory.TestPassword),
                Role = PanelRole.Implementer,
            });
            await db.SaveChangesAsync();
        }

        var login = await factory.PanelLoginAsync(email, ApiFactory.TestPassword);
        return factory.CreateClient(login.AccessToken);
    }

    private static async Task<Guid> DemoAccountIdAsync()
    {
        await using var db = ApiFactory.CreateDbContext();
        return (await db.Accounts.SingleAsync(a => a.Code == DemoDataSeeder.AccountCode)).Id;
    }

    private Task<HttpResponseMessage> LoginAsync(string login, string password) =>
        factory.CreateClient().PostAsJsonAsync("/api/erp/auth/login", new LoginRequest(login, password));

    private async Task<(Guid Id, string Email)> CreateUserAsync()
    {
        var username = $"panel.{Guid.NewGuid():N}"[..20];
        var email = $"{username}@demopharma.com";
        var hasher = factory.Services.GetRequiredService<IPasswordHasher>();
        await using var db = ApiFactory.CreateDbContext();
        var demo = await db.Accounts.SingleAsync(a => a.Code == DemoDataSeeder.AccountCode);
        var viewer = await db.Roles.IgnoreQueryFilters().SingleAsync(r => r.AccountId == demo.Id && r.Name == DemoDataSeeder.ViewerRole);

        var user = new ErpUser
        {
            AccountId = demo.Id,
            Username = username,
            Email = email,
            FullName = "Panel Managed",
            PasswordHash = hasher.Hash(ApiFactory.TestPassword),
        };
        user.BusinessUnits.Add(new UserBusinessUnit { AccountId = demo.Id, BusinessUnitId = factory.DhakaId, RoleId = viewer.Id });
        db.ErpUsers.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, email);
    }
}
