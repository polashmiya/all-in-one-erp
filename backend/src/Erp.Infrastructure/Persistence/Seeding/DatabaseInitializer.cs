using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Core.Modules;
using Erp.Infrastructure.Metadata;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Erp.Infrastructure.Persistence.Seeding;

public static class DatabaseInitializer
{
    /// <summary>Migrate (if enabled), synchronise the permission catalog, then seed demo data (if enabled).</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var db = provider.GetRequiredService<ErpDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (options.MigrateOnStartup)
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
                await db.Database.MigrateAsync(cancellationToken);
            }
        }

        await SyncPermissionCatalogAsync(db, provider.GetRequiredService<PermissionCatalog>(), logger, cancellationToken);
        await PanelMenuSynchronizer.SyncAsync(db, logger, cancellationToken);
        await ErpMenuSynchronizer.SyncAsync(db, provider.GetRequiredService<ErpMenuCatalog>(), logger, cancellationToken);
        await FeatureManifestSynchronizer.SyncAsync(db, provider.GetRequiredService<FeatureManifestCatalog>(), logger, cancellationToken);

        if (options.SeedDemoData)
        {
            await provider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
        }

        var lookups = provider.GetRequiredService<LookupDefaults>();
        await lookups.SyncTypesAsync(cancellationToken);
        foreach (var accountId in await db.Accounts.Select(a => a.Id).ToListAsync(cancellationToken))
        {
            await lookups.AddMissingAsync(accountId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Makes the Permissions table match the catalog: new codes are inserted, descriptions refreshed, and codes no
    /// module declares any more are deleted (their role grants go with them).
    /// </summary>
    private static async Task SyncPermissionCatalogAsync(
        ErpDbContext db, PermissionCatalog catalog, ILogger logger, CancellationToken cancellationToken)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);
        var declared = catalog.Codes.ToHashSet(StringComparer.Ordinal);
        var obsolete = existing.Values.Where(p => !declared.Contains(p.Code)).ToList();
        db.Permissions.RemoveRange(obsolete);
        var added = 0;

        foreach (var definition in catalog.All)
        {
            if (existing.TryGetValue(definition.Code.Value, out var row))
            {
                row.Description = definition.Description;
                continue;
            }

            db.Permissions.Add(new Permission
            {
                Code = definition.Code.Value,
                Module = definition.Code.Module,
                Feature = definition.Code.Feature,
                Action = definition.Code.Action,
                Description = definition.Description,
            });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        if (added > 0) logger.LogInformation("Permission catalog: added {Count} permission(s)", added);
        if (obsolete.Count > 0) logger.LogInformation("Permission catalog: removed {Codes}", string.Join(", ", obsolete.Select(p => p.Code)));
    }
}
