using System.Net;
using System.Net.Http.Json;
using Erp.Api.Auth;
using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SessionTests(ApiFactory factory)
{
    [Fact]
    public async Task Login_returns_a_refresh_token_that_rotates()
    {
        var email = await CreateUserAsync();
        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);

        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.True(login.RefreshExpiresAt > login.ExpiresAt);

        var next = await RefreshAsync(login.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        var tokens = (await next.Content.ReadFromJsonAsync<SessionTokens>())!;
        Assert.NotEqual(login.RefreshToken, tokens.RefreshToken);

        var me = await factory.CreateClient(tokens.AccessToken).GetAsync("/api/erp/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_ends_the_whole_session()
    {
        var email = await CreateUserAsync();
        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);
        var next = (await (await RefreshAsync(login.RefreshToken)).Content.ReadFromJsonAsync<SessionTokens>())!;

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(login.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(next.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var email = await CreateUserAsync();
        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);

        var logout = await factory.CreateClient(login.AccessToken).PostAsync("/api/erp/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(login.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Three_wrong_passwords_lock_the_user()
    {
        var email = await CreateUserAsync();
        for (var i = 0; i < 3; i++) await LoginAsync(email, "Wrong@12345");

        var response = await LoginAsync(email, ApiFactory.TestPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var db = ApiFactory.CreateDbContext();
        var user = await db.ErpUsers.IgnoreQueryFilters().SingleAsync(u => u.Email == email);
        Assert.NotNull(user.LockedUntil);
        var results = await db.LoginEvents.Where(e => e.UserId == user.Id).Select(e => e.Result).ToListAsync();
        Assert.Contains(LoginResults.Locked, results);
    }

    [Fact]
    public async Task A_user_who_must_change_the_password_can_do_nothing_else_until_they_do()
    {
        var email = await CreateUserAsync(mustChange: true);
        var login = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);
        Assert.True(login.MustChangePassword);

        var client = factory.CreateClient(login.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/erp/navigation")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/erp/auth/me")).StatusCode);

        var change = await client.PostAsJsonAsync("/api/erp/auth/change-password",
            new ChangePasswordRequest(ApiFactory.TestPassword, "Changed@12345"));
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var tokens = (await change.Content.ReadFromJsonAsync<SessionTokens>())!;
        Assert.False(tokens.MustChangePassword);

        var after = await factory.CreateClient(tokens.AccessToken).GetAsync("/api/erp/navigation");
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    [Fact]
    public async Task Changing_the_password_ends_other_sessions_at_once()
    {
        var email = await CreateUserAsync();
        var first = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);
        var second = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);

        var change = await factory.CreateClient(first.AccessToken).PostAsJsonAsync("/api/erp/auth/change-password",
            new ChangePasswordRequest(ApiFactory.TestPassword, "Changed@12345"));
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient(second.AccessToken).GetAsync("/api/erp/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Change_password_rejects_a_wrong_current_password_and_a_weak_new_one()
    {
        var email = await CreateUserAsync();
        var client = factory.CreateClient((await factory.ErpLoginAsync(email, ApiFactory.TestPassword)).AccessToken);

        var wrong = await client.PostAsJsonAsync("/api/erp/auth/change-password",
            new ChangePasswordRequest("Wrong@12345", "Changed@12345"));
        var weak = await client.PostAsJsonAsync("/api/erp/auth/change-password",
            new ChangePasswordRequest(ApiFactory.TestPassword, "short"));

        Assert.Contains("currentPassword", (await wrong.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Contains("newPassword", (await weak.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task Panel_sessions_refresh_too()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/panel/auth/refresh", new RefreshRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        factory.CreateClient().PostAsJsonAsync("/api/erp/auth/refresh", new RefreshRequest(refreshToken));

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        factory.CreateClient().PostAsJsonAsync("/api/erp/auth/login", new LoginRequest(email, password));

    private async Task<string> CreateUserAsync(bool mustChange = false)
    {
        var email = $"session.{Guid.NewGuid():N}@demopharma.com";
        var hasher = factory.Services.GetRequiredService<IPasswordHasher>();
        await using var db = ApiFactory.CreateDbContext();
        var demo = await db.Accounts.SingleAsync(a => a.Code == DemoDataSeeder.AccountCode);
        var viewer = await db.Roles.IgnoreQueryFilters().SingleAsync(r => r.AccountId == demo.Id && r.Name == DemoDataSeeder.ViewerRole);

        var user = new ErpUser
        {
            AccountId = demo.Id,
            Username = email.Split('@')[0],
            Email = email,
            FullName = "Session User",
            PasswordHash = hasher.Hash(ApiFactory.TestPassword),
            MustChangePassword = mustChange,
        };
        user.BusinessUnits.Add(new UserBusinessUnit { AccountId = demo.Id, BusinessUnitId = factory.DhakaId, RoleId = viewer.Id });
        db.ErpUsers.Add(user);
        await db.SaveChangesAsync();
        return email;
    }
}
