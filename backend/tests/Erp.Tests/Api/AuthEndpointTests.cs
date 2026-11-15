using System.Net;
using System.Net.Http.Json;
using Erp.Api.Auth;
using Erp.Modules.Inventory;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Panel_login_with_seeded_super_admin_returns_token_and_role()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);

        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.True(login.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal("SuperAdmin", login.User.Role);

        var me = await factory.CreateClient(login.AccessToken).GetFromJsonAsync<PanelUserDto>("/api/panel/auth/me");
        Assert.Equal(factory.Seed.PanelAdmin.Email, me!.Email);
    }

    [Theory]
    [InlineData("/api/panel/auth/login", "superadmin@myerp.local", "wrong-password")]
    [InlineData("/api/panel/auth/login", "nobody@myerp.local", "Admin@12345")]
    [InlineData("/api/erp/auth/login", "admin@demopharma.com", "wrong-password")]
    [InlineData("/api/erp/auth/login", "nobody@demopharma.com", "Demo@12345")]
    public async Task Bad_credentials_get_the_same_401_problem(string url, string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync(url, new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Invalid email or password", problem!.Title);
    }

    [Fact]
    public async Task Login_validation_errors_are_400_with_camel_case_keys()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/erp/auth/login", new LoginRequest("", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("login", problem!.Errors.Keys);
        Assert.Contains("password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Erp_login_returns_both_depots_for_the_demo_admin()
    {
        var login = await factory.ErpAdminLoginAsync();

        Assert.Equal("Demo Pharma Ltd", login.User.AccountName);
        Assert.True(login.User.IsAccountAdmin);
        Assert.Equal(["Chattogram Depot", "Dhaka Depot"], login.BusinessUnits.Select(u => u.Name));
        Assert.All(login.BusinessUnits, u => Assert.Equal("Administrator", u.RoleName));
    }

    [Fact]
    public async Task Erp_login_lists_only_assigned_units_for_a_regular_user()
    {
        var login = await factory.ErpLoginAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword);

        var unit = Assert.Single(login.BusinessUnits);
        Assert.Equal("DHK", unit.Code);
        Assert.Equal("Viewer", unit.RoleName);
    }

    [Fact]
    public async Task Panel_token_is_rejected_by_the_erp_api()
    {
        var panel = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);

        var response = await factory.CreateClient(panel.AccessToken).GetAsync("/api/erp/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Erp_token_is_rejected_by_the_panel_api()
    {
        var erp = await factory.ErpAdminLoginAsync();

        var response = await factory.CreateClient(erp.AccessToken).GetAsync("/api/panel/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/erp/auth/me")]
    [InlineData("/api/panel/auth/me")]
    public async Task Missing_token_is_401(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Erp_session_returns_user_units_and_last_used_unit()
    {
        var login = await factory.ErpAdminLoginAsync();
        var client = factory.CreateClient(login.AccessToken);
        (await client.PostAsJsonAsync("/api/erp/auth/select-unit", new SelectUnitRequest(factory.ChattogramId))).EnsureSuccessStatusCode();

        var session = await client.GetFromJsonAsync<ErpSessionResponse>("/api/erp/auth/me");

        Assert.Equal(factory.Seed.ErpAdmin.Email, session!.User.Email);
        Assert.Equal(2, session.BusinessUnits.Count);
        Assert.Equal(factory.ChattogramId, session.LastBusinessUnitId);
    }

    [Fact]
    public async Task Select_unit_returns_role_and_every_permission_for_an_account_admin()
    {
        var login = await factory.ErpAdminLoginAsync();

        var response = await factory.CreateClient(login.AccessToken)
            .PostAsJsonAsync("/api/erp/auth/select-unit", new SelectUnitRequest(factory.DhakaId));

        response.EnsureSuccessStatusCode();
        var selected = await response.Content.ReadFromJsonAsync<SelectUnitResponse>();
        Assert.Equal("Dhaka Depot", selected!.BusinessUnit.Name);
        Assert.Equal(49, selected.Permissions.Count); // 6 item screens x 6 actions, user and employee x 4, user role x 5
        Assert.Contains($"{ItemManagementMenu.Uom}.import", selected.Permissions);
    }

    [Fact]
    public async Task Select_unit_returns_only_role_permissions_for_a_regular_user()
    {
        var login = await factory.ErpLoginAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword);

        var response = await factory.CreateClient(login.AccessToken)
            .PostAsJsonAsync("/api/erp/auth/select-unit", new SelectUnitRequest(factory.DhakaId));

        var selected = await response.Content.ReadFromJsonAsync<SelectUnitResponse>();
        Assert.Equal(18, selected!.Permissions.Count); // view + export for each of the 9 features
        Assert.All(selected.Permissions, code => Assert.True(code.EndsWith(".view") || code.EndsWith(".export")));
    }

    [Theory]
    [InlineData(nameof(ApiFactory.ChattogramId))] // same account, not assigned
    [InlineData(nameof(ApiFactory.OtherAccountUnitId))] // different account
    public async Task Select_unit_without_access_is_403(string unit)
    {
        var login = await factory.ErpLoginAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword);
        var unitId = (Guid)typeof(ApiFactory).GetProperty(unit)!.GetValue(factory)!;

        var response = await factory.CreateClient(login.AccessToken)
            .PostAsJsonAsync("/api/erp/auth/select-unit", new SelectUnitRequest(unitId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
