using Erp.Core.Authorization;
using Erp.Core.Modules;
using Erp.Core.Security;
using Erp.Infrastructure.Authorization;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Infrastructure.Security;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Erp.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Erp";

    /// <summary>
    /// Persistence, authorization and security services. The host must register <c>ICurrentContext</c>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                "Set ConnectionStrings:Erp in appsettings.Development.json or the ConnectionStrings__Erp environment variable.");
        }

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Section));
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<AuditingInterceptor>();
        services.AddDbContext<ErpDbContext>((provider, options) => options
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ErpDbContext).Assembly.FullName))
            .AddInterceptors(provider.GetRequiredService<AuditingInterceptor>()));

        services.AddSingleton<ErpMenuCatalog>();
        services.AddSingleton<FeatureManifestCatalog>();
        services.AddMemoryCache();
        services.AddScoped<FeatureConfigService>();
        services.AddSingleton<IPermissionProvider, MenuPermissionProvider>();
        services.AddSingleton<PermissionCatalog>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IBusinessUnitAccess, BusinessUnitAccess>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<DemoDataSeeder>();

        services.AddHealthChecks()
            .AddDbContextCheck<ErpDbContext>("database", tags: ["db"]);

        return services;
    }
}
