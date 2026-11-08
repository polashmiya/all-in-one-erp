using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Erp.Infrastructure.Metadata;

public sealed class FeatureConfigService(ErpDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(30);

    public async Task<ResolvedFeature?> GetAsync(Guid accountId, string featureCode, CancellationToken cancellationToken)
    {
        var code = featureCode.ToLowerInvariant();
        var grant = await db.AccountFeatures.AcrossTenants().AsNoTracking()
            .Where(af => af.AccountId == accountId && af.Feature!.Code == code && af.Feature.IsActive)
            .Select(af => new { af.FeatureId, af.ConfigVersion })
            .SingleOrDefaultAsync(cancellationToken);
        if (grant is null) return null;

        return await cache.GetOrCreateAsync((nameof(FeatureConfig), accountId, code, grant.ConfigVersion), entry =>
        {
            entry.SlidingExpiration = CacheFor;
            return LoadAsync(accountId, grant.FeatureId, code, grant.ConfigVersion, cancellationToken);
        });
    }

    public async Task<(T Request, Dictionary<string, string[]> Errors)> ApplyRulesAsync<T>(
        Guid accountId, string featureCode, T request, object? existing, CancellationToken cancellationToken) =>
        await GetAsync(accountId, featureCode, cancellationToken) is { } feature
            ? FieldRules.Apply(request, feature.Rules, existing)
            : (request, []);

    private async Task<ResolvedFeature?> LoadAsync(Guid accountId, Guid featureId, string code, int version, CancellationToken cancellationToken)
    {
        var fields = await db.FeatureFields.AsNoTracking().Where(f => f.FeatureId == featureId).ToListAsync(cancellationToken);
        if (fields.Count == 0) return null;

        var sections = await db.FeatureSections.AsNoTracking().Where(s => s.FeatureId == featureId).ToListAsync(cancellationToken);
        var sectionSettings = await db.AccountSectionSettings.AcrossTenants().AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Section!.FeatureId == featureId)
            .ToListAsync(cancellationToken);
        var fieldSettings = await db.AccountFieldSettings.AcrossTenants().AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Field!.FeatureId == featureId)
            .ToListAsync(cancellationToken);

        return FeatureConfigBuilder.Build(code, version, sections, fields, sectionSettings, fieldSettings);
    }
}
