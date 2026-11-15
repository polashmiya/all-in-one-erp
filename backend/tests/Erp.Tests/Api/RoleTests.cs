using System.Net;
using System.Net.Http.Json;
using Erp.Api.Auth;
using Erp.Api.UserManagement;
using Erp.Api.UserManagement.Roles;
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
public sealed class RoleTests(ApiFactory factory)
{
    private const string RolesUrl = "/api/erp/user-management/roles";
    private static readonly string ItemProfileView = $"{ItemManagementMenu.ItemProfile}.view";
    private static readonly string ItemProfileCreate = $"{ItemManagementMenu.ItemProfile}.create";

    [Fact]
    public async Task The_permission_tree_lists_user_management_screens_with_their_actions()
    {
        var tree = await (await AdminAsync()).GetFromJsonAsync<List<PermissionTreeModule>>("/api/erp/user-management/permission-tree");

        var setup = Assert.Single(tree!, m => m.Key == UserManagementMenu.Module);
        var roles = setup.SubModules.SelectMany(s => s.Features).Single(f => f.Code == UserManagementMenu.UserRole);
        Assert.Contains("delete", roles.Actions);
        Assert.Contains(setup.SubModules, s => s.Key == "item-management");
    }

    [Fact]
    public async Task A_role_is_created_listed_and_rejects_a_duplicate_name_or_unknown_permission()
    {
        var admin = await AdminAsync();
        var name = Unique("Store");

        var created = await admin.PostAsJsonAsync(RolesUrl, new SaveRoleRequest(name, "Items", true, [ItemProfileView, ItemProfileCreate]));
        var detail = (await created.Content.ReadFromJsonAsync<RoleDetail>())!;
        var duplicate = await admin.PostAsJsonAsync(RolesUrl, new SaveRoleRequest(name, null, true, []));
        var unknown = await admin.PostAsJsonAsync(RolesUrl, new SaveRoleRequest(Unique("Bad"), null, true, ["nope.screen.view"]));
        var list = await admin.GetFromJsonAsync<RoleList>($"{RolesUrl}?search={name}");

        Assert.Equal([ItemProfileCreate, ItemProfileView], detail.Permissions);
        Assert.Contains("name", (await duplicate.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Contains("permissions", (await unknown.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Equal(2, Assert.Single(list!.Items).PermissionCount);
    }

    [Fact]
    public async Task Administrator_has_every_permission_and_cannot_be_changed_or_deleted()
    {
        var admin = await AdminAsync();
        var list = await admin.GetFromJsonAsync<RoleList>($"{RolesUrl}?search={DemoDataSeeder.AdministratorRole}");
        var administrator = list!.Items.Single(r => r.Name == DemoDataSeeder.AdministratorRole);

        var update = await admin.PutAsJsonAsync($"{RolesUrl}/{administrator.Id}", new SaveRoleRequest("Administrator", null, true, []));
        var delete = await admin.DeleteAsync($"{RolesUrl}/{administrator.Id}");

        Assert.True(administrator.IsAdministrator);
        Assert.Equal(list.TotalPermissions, administrator.PermissionCount);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    [Fact]
    public async Task Assigning_a_role_changes_the_users_permissions_in_that_unit_only()
    {
        var admin = await AdminAsync();
        var role = await CreateRoleAsync(admin, [ItemProfileView, ItemProfileCreate]);
        var (userId, email) = await CreateUserAsync(null);

        var assign = await admin.PutAsJsonAsync($"{RolesUrl}/{role.Id}/users", new RoleUsersRequest([userId]));
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);
        var selected = await factory.CreateClient(login.AccessToken)
            .PostAsJsonAsync("/api/erp/auth/select-unit", new SelectUnitRequest(factory.DhakaId));
        var permissions = (await selected.Content.ReadFromJsonAsync<SelectUnitResponse>())!.Permissions;
        Assert.Equal([ItemProfileCreate, ItemProfileView], permissions);

        var detail = await admin.GetFromJsonAsync<RoleDetail>($"{RolesUrl}/{role.Id}");
        Assert.Equal(userId, Assert.Single(detail!.Users).UserId);
    }

    [Fact]
    public async Task A_user_without_a_role_in_a_unit_is_not_offered_that_unit()
    {
        var (_, email) = await CreateUserAsync(null);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/erp/auth/login", new LoginRequest(email, ApiFactory.TestPassword));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Users_cannot_give_permissions_they_do_not_have()
    {
        var admin = await AdminAsync();
        var manager = await CreateRoleAsync(admin,
            [$"{UserManagementMenu.UserRole}.view", $"{UserManagementMenu.UserRole}.create", ItemProfileView]);
        var (_, email) = await CreateUserAsync(manager.Id);
        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);
        var client = factory.CreateClient(login.AccessToken, factory.DhakaId);

        var allowed = await client.PostAsJsonAsync(RolesUrl, new SaveRoleRequest(Unique("Small"), null, true, [ItemProfileView]));
        var escalated = await client.PostAsJsonAsync(RolesUrl, new SaveRoleRequest(Unique("Big"), null, true, [ItemProfileCreate]));

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Contains("permissions", (await escalated.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task An_inactive_role_gives_nothing_and_a_used_role_cannot_be_deleted()
    {
        var admin = await AdminAsync();
        var role = await CreateRoleAsync(admin, [ItemProfileView]);
        var (_, email) = await CreateUserAsync(role.Id);

        await admin.PutAsJsonAsync($"{RolesUrl}/{role.Id}/status", new RoleStatusRequest(false));
        var delete = await admin.DeleteAsync($"{RolesUrl}/{role.Id}");

        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);
        var selected = await factory.CreateClient(login.AccessToken)
            .PostAsJsonAsync("/api/erp/auth/select-unit", new SelectUnitRequest(factory.DhakaId));
        Assert.Empty((await selected.Content.ReadFromJsonAsync<SelectUnitResponse>())!.Permissions);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..20];

    private async Task<HttpClient> AdminAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private static async Task<RoleDetail> CreateRoleAsync(HttpClient admin, string[] permissions)
    {
        var response = await admin.PostAsJsonAsync(RolesUrl, new SaveRoleRequest(Unique("Role"), null, true, permissions));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoleDetail>())!;
    }

    private async Task<(Guid Id, string Email)> CreateUserAsync(Guid? roleId)
    {
        var username = $"role.{Guid.NewGuid():N}"[..20];
        var email = $"{username}@demopharma.com";
        var hasher = factory.Services.GetRequiredService<IPasswordHasher>();
        await using var db = ApiFactory.CreateDbContext();
        var demo = await db.Accounts.SingleAsync(a => a.Code == DemoDataSeeder.AccountCode);

        var user = new ErpUser
        {
            AccountId = demo.Id,
            Username = username,
            Email = email,
            FullName = "Role User",
            PasswordHash = hasher.Hash(ApiFactory.TestPassword),
        };
        user.BusinessUnits.Add(new UserBusinessUnit { AccountId = demo.Id, BusinessUnitId = factory.DhakaId, RoleId = roleId });
        db.ErpUsers.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, email);
    }
}
