using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Api.Auth;
using Erp.Api.Hosting;
using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Tenancy;
using Erp.Core.Modules;
using Erp.Core.Notifications;
using Erp.Core.Security;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Erp.Tests.Infrastructure;

/// <summary>
/// Boots the real API against a throwaway LocalDB database (never the developer's myErp database): migrations and
/// demo seed run exactly as in development, then a second tenant and a limited user are added for isolation tests.
/// Override the server with the ERP_TEST_CONNECTION environment variable, e.g. in CI.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=myErp_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30";

    public const string ViewerEmail = "viewer@demopharma.com";
    public const string NewUserEmail = "new.user@demopharma.com";
    public const string OtherAdminEmail = "admin@othertrading.com";
    public const string TestPassword = "Test@12345";

    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("ERP_TEST_CONNECTION") ?? DefaultConnection;

    public SeedOptions Seed => Services.GetRequiredService<IOptions<SeedOptions>>().Value;

    public CapturedEmails Emails { get; } = new();

    public Guid DhakaId { get; private set; }

    public Guid ChattogramId { get; private set; }

    /// <summary>A business unit of a different account ("Other Trading").</summary>
    public Guid OtherAccountUnitId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Erp", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", "true");
        builder.UseSetting("RateLimiting:LoginPerMinute", "10000");
        builder.UseSetting("Auth:RefreshReuseGrace", "00:00:00");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IErpEndpoints, ProbeEndpoints>();
            services.AddSingleton(Emails);
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public async Task InitializeAsync()
    {
        await using (var db = CreateDbContext())
        {
            await db.Database.EnsureDeletedAsync();
        }

        _ = Services; // starts the host: migrate + permission sync + demo seed
        await SeedTestDataAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var db = CreateDbContext())
        {
            await db.Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }

    public static ErpDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpDbContext>().UseSqlServer(ConnectionString).Options, SystemCurrentContext.Instance);

    public HttpClient CreateClient(string? accessToken, Guid? businessUnitId = null)
    {
        var client = CreateClient();
        if (accessToken is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (businessUnitId is not null) client.DefaultRequestHeaders.Add(BusinessUnitHeaderFilter.HeaderName, businessUnitId.ToString());
        return client;
    }

    public async Task<ErpLoginResponse> ErpLoginAsync(string email, string password)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/erp/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ErpLoginResponse>())!;
    }

    public async Task<PanelLoginResponse> PanelLoginAsync(string email, string password)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/panel/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PanelLoginResponse>())!;
    }

    /// <summary>Signs in and, when the user must change the password first, changes it to <see cref="ChangedPassword"/>.</summary>
    public async Task<ErpLoginResponse> ErpReadyLoginAsync(string login, string password)
    {
        var signIn = await ErpLoginAsync(login, password);
        if (!signIn.MustChangePassword) return signIn;

        var response = await CreateClient(signIn.AccessToken)
            .PostAsJsonAsync("/api/erp/auth/change-password", new ChangePasswordRequest(password, ChangedPassword));
        response.EnsureSuccessStatusCode();
        var tokens = (await response.Content.ReadFromJsonAsync<SessionTokens>())!;
        return signIn with { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken, MustChangePassword = false };
    }

    public const string ChangedPassword = "Changed@12345";

    public Task<ErpLoginResponse> ErpAdminLoginAsync() => ErpLoginAsync(Seed.ErpAdmin.Email, Seed.ErpAdmin.Password);

    private async Task SeedTestDataAsync()
    {
        var hasher = Services.GetRequiredService<IPasswordHasher>();
        await using var db = CreateDbContext();

        var demo = await db.Accounts.SingleAsync(a => a.Code == DemoDataSeeder.AccountCode);
        var units = await db.BusinessUnits.IgnoreQueryFilters().Where(b => b.AccountId == demo.Id).ToListAsync();
        DhakaId = units.Single(u => u.Code == "DHK").Id;
        ChattogramId = units.Single(u => u.Code == "CTG").Id;
        var viewer = await db.Roles.IgnoreQueryFilters().SingleAsync(r => r.AccountId == demo.Id && r.Name == DemoDataSeeder.ViewerRole);

        // Viewer: Dhaka only, read-only role.
        var viewerUser = new ErpUser
        {
            AccountId = demo.Id,
            Username = "viewer.user",
            Email = ViewerEmail,
            FullName = "Viewer User",
            PasswordHash = hasher.Hash(TestPassword),
        };
        viewerUser.BusinessUnits.Add(new UserBusinessUnit { AccountId = demo.Id, BusinessUnitId = DhakaId, RoleId = viewer.Id });
        db.ErpUsers.Add(viewerUser);

        // A new user whose role has no permissions yet: the admin has not given them anything.
        var noAccess = new Role { AccountId = demo.Id, Name = "No access yet" };
        var newUser = new ErpUser
        {
            AccountId = demo.Id,
            Username = "new.user",
            Email = NewUserEmail,
            FullName = "New User",
            PasswordHash = hasher.Hash(TestPassword),
        };
        newUser.BusinessUnits.Add(new UserBusinessUnit { AccountId = demo.Id, BusinessUnitId = DhakaId, Role = noAccess });
        db.AddRange(noAccess, newUser);

        // A second tenant with its own unit and primary admin.
        var other = new Account { Code = "OTHER", Name = "Other Trading" };
        var otherUnit = new BusinessUnit { Account = other, Code = "OTH", Name = "Other Depot" };
        db.AddRange(other, otherUnit);
        db.ErpUsers.Add(new ErpUser
        {
            Account = other,
            Username = "other.admin",
            Email = OtherAdminEmail,
            FullName = "Other Admin",
            PasswordHash = hasher.Hash(TestPassword),
            IsAccountAdmin = true,
        });

        await db.SaveChangesAsync();
        OtherAccountUnitId = otherUnit.Id;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
