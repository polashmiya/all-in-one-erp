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
public sealed class PasswordResetTests(ApiFactory factory)
{
    [Fact]
    public async Task Users_sign_in_with_email_or_username()
    {
        var (email, username) = await CreateUserAsync();

        var byEmail = await LoginAsync(email, ApiFactory.TestPassword);
        var byUsername = await LoginAsync(username.ToUpperInvariant(), ApiFactory.TestPassword);

        Assert.Equal(HttpStatusCode.OK, byEmail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byUsername.StatusCode);
    }

    [Fact]
    public async Task A_reset_code_sets_a_new_password_and_ends_old_sessions()
    {
        var (email, username) = await CreateUserAsync();
        var before = await factory.ErpLoginAsync(email, ApiFactory.TestPassword);

        var forgot = await Client().PostAsJsonAsync("/api/erp/auth/forgot-password", new ForgotPasswordRequest(username, null));
        Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);
        var code = factory.Emails.LastCodeFor(email);

        var reset = await Client().PostAsJsonAsync("/api/erp/auth/reset-password",
            new ResetPasswordRequest(username, code, "Reset@12345"));

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, "Reset@12345")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient(before.AccessToken).GetAsync("/api/erp/auth/me")).StatusCode);
    }

    [Fact]
    public async Task A_wrong_code_is_rejected_and_a_used_code_cannot_be_used_again()
    {
        var (email, _) = await CreateUserAsync();
        await Client().PostAsJsonAsync("/api/erp/auth/forgot-password", new ForgotPasswordRequest(email, "bn"));
        var code = factory.Emails.LastCodeFor(email);
        var wrong = code == "000000" ? "111111" : "000000";

        var rejected = await Client().PostAsJsonAsync("/api/erp/auth/reset-password", new ResetPasswordRequest(email, wrong, "Reset@12345"));
        var accepted = await Client().PostAsJsonAsync("/api/erp/auth/reset-password", new ResetPasswordRequest(email, code, "Reset@12345"));
        var again = await Client().PostAsJsonAsync("/api/erp/auth/reset-password", new ResetPasswordRequest(email, code, "Again@12345"));

        Assert.Contains("code", (await rejected.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_answers_the_same_for_unknown_users_and_does_not_resend_at_once()
    {
        var (email, _) = await CreateUserAsync();

        var unknown = await Client().PostAsJsonAsync("/api/erp/auth/forgot-password", new ForgotPasswordRequest("nobody.here", null));
        await Client().PostAsJsonAsync("/api/erp/auth/forgot-password", new ForgotPasswordRequest(email, null));
        await Client().PostAsJsonAsync("/api/erp/auth/forgot-password", new ForgotPasswordRequest(email, null));

        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
        Assert.Equal(1, factory.Emails.CountFor(email));
    }

    [Fact]
    public async Task A_successful_reset_unlocks_the_user()
    {
        var (email, _) = await CreateUserAsync();
        for (var i = 0; i < 3; i++) await LoginAsync(email, "Wrong@12345");

        await Client().PostAsJsonAsync("/api/erp/auth/forgot-password", new ForgotPasswordRequest(email, null));
        await Client().PostAsJsonAsync("/api/erp/auth/reset-password",
            new ResetPasswordRequest(email, factory.Emails.LastCodeFor(email), "Reset@12345"));

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, "Reset@12345")).StatusCode);
    }

    private HttpClient Client() => factory.CreateClient();

    private Task<HttpResponseMessage> LoginAsync(string login, string password) =>
        Client().PostAsJsonAsync("/api/erp/auth/login", new LoginRequest(login, password));

    private async Task<(string Email, string Username)> CreateUserAsync()
    {
        var username = $"reset.{Guid.NewGuid():N}"[..20];
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
            FullName = "Reset User",
            PasswordHash = hasher.Hash(ApiFactory.TestPassword),
        };
        user.BusinessUnits.Add(new UserBusinessUnit { AccountId = demo.Id, BusinessUnitId = factory.DhakaId, RoleId = viewer.Id });
        db.ErpUsers.Add(user);
        await db.SaveChangesAsync();
        return (email, username);
    }
}
