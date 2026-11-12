using Erp.Api.Auth;
using Erp.Api.Endpoints;
using Erp.Api.Hosting;
using Erp.Api.Metadata;
using Erp.Api.Navigation;
using Erp.Api.Panel.Accounts;
using Erp.Api.Lookups;
using Erp.Api.Panel.Configuration;
using Erp.Api.Panel.Lookups;
using Erp.Api.Panel.Navigation;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Infrastructure;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Modules.Inventory;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddProblemDetails();
builder.Services.AddErpOpenApi();
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpCurrentContext>();
builder.Services.AddScoped<ICurrentContext>(provider => provider.GetRequiredService<HttpCurrentContext>());
builder.Services.AddErpAuthentication(builder.Configuration);
builder.Services.AddRequestValidation();
builder.Services.AddLoginRateLimiting(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInventoryModule();
builder.Services.AddScoped<AccountSetupService>();
builder.Services.AddScoped<AccountFeatureService>();
builder.Services.AddScoped<FeatureConfigurationService>();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(CorsSetup.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference("/docs", options => options
    .WithTitle("myErp API")
    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch));

app.MapHealthEndpoints();
app.MapSystemEndpoints();

var panel = app.MapPanelApi();
panel.MapPanelAuthEndpoints();
panel.MapPanelNavigationEndpoints();
panel.MapAccountEndpoints();
panel.MapPanelErpMenuEndpoints();
panel.MapFeatureConfigurationEndpoints();
panel.MapLookupListEndpoints();
panel.MapConfigurationHistoryEndpoints();
foreach (var endpoints in app.Services.GetServices<IPanelEndpoints>()) endpoints.Map(panel);

var erp = app.MapErpApi();
erp.MapErpAuthEndpoints();
erp.MapErpNavigationEndpoints();
erp.MapFeatureConfigEndpoints();
erp.MapLookupEndpoints();
foreach (var endpoints in app.Services.GetServices<IErpEndpoints>()) endpoints.Map(erp);

app.Run();

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
