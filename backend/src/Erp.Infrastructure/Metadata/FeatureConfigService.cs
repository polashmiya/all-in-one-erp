using System.Globalization;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Erp.Infrastructure.Metadata;

public sealed class FeatureConfigService(ErpDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(30);

    public async Task<ResolvedFeature?> GetAsync(
        Guid accountId, Guid? businessUnitId, string featureCode, CancellationToken cancellationToken)
    {
        var code = featureCode.ToLowerInvariant();
        var grant = await db.AccountFeatures.AcrossTenants().AsNoTracking()
            .Where(af => af.AccountId == accountId && af.Feature!.Code == code && af.Feature.IsActive)
            .Select(af => new { af.FeatureId, af.ConfigVersion })
            .SingleOrDefaultAsync(cancellationToken);
        if (grant is null) return null;

        return await cache.GetOrCreateAsync((nameof(FeatureConfig), accountId, businessUnitId, code, grant.ConfigVersion), entry =>
        {
            entry.SlidingExpiration = CacheFor;
            return LoadAsync(accountId, businessUnitId, grant.FeatureId, code, grant.ConfigVersion, cancellationToken);
        });
    }

    public async Task<(T Request, Dictionary<string, string[]> Errors)> ApplyRulesAsync<T>(
        Guid accountId, Guid businessUnitId, string featureCode, T request, object? existing, CancellationToken cancellationToken) =>
        await GetAsync(accountId, businessUnitId, featureCode, cancellationToken) is { } feature
            ? FieldRules.Apply(request, feature.Rules, existing, await CustomLookupValuesAsync(accountId, feature.Rules, cancellationToken))
            : (request, []);

    private async Task<Dictionary<string, HashSet<int>>> CustomLookupValuesAsync(
        Guid accountId, IReadOnlyList<FieldRule> rules, CancellationToken cancellationToken)
    {
        var lists = rules.Where(r => r.Field.Custom).Select(r => r.Field.Lookup).OfType<string>().Distinct().ToList();
        if (lists.Count == 0) return [];

        var values = await db.LookupValues.AcrossTenants().AsNoTracking()
            .Where(v => v.AccountId == accountId && v.IsActive && lists.Contains(v.LookupType!.Key))
            .Select(v => new { v.LookupType!.Key, v.Id })
            .ToListAsync(cancellationToken);
        return values.GroupBy(v => v.Key).ToDictionary(g => g.Key, g => g.Select(v => v.Id).ToHashSet());
    }

    private async Task<ResolvedFeature?> LoadAsync(
        Guid accountId, Guid? businessUnitId, Guid featureId, string code, int version, CancellationToken cancellationToken)
    {
        var fields = await db.FeatureFields.AsNoTracking()
            .Where(f => f.FeatureId == featureId && (f.AccountId == null || f.AccountId == accountId))
            .ToListAsync(cancellationToken);
        if (fields.Count == 0) return null;

        var sections = await db.FeatureSections.AsNoTracking().Where(s => s.FeatureId == featureId).ToListAsync(cancellationToken);
        var sectionSettings = await db.AccountSectionSettings.AcrossTenants().AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Section!.FeatureId == featureId)
            .Where(s => s.BusinessUnitId == null || s.BusinessUnitId == businessUnitId)
            .ToListAsync(cancellationToken);
        var fieldSettings = await db.AccountFieldSettings.AcrossTenants().AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Field!.FeatureId == featureId)
            .Where(s => s.BusinessUnitId == null || s.BusinessUnitId == businessUnitId)
            .ToListAsync(cancellationToken);

        return FeatureConfigBuilder.Build(
            code,
            version,
            sections,
            fields,
            FeatureSettings.ForUnit(sectionSettings, businessUnitId),
            FeatureSettings.ForUnit(fieldSettings, businessUnitId),
            await LookupDefaultsAsync(db, accountId, cancellationToken));
    }

    public static async Task<Dictionary<string, string>> LookupDefaultsAsync(ErpDbContext db, Guid accountId, CancellationToken ct)
    {
        var defaults = await db.LookupValues.AcrossTenants().AsNoTracking()
            .Where(v => v.AccountId == accountId && v.IsDefault && v.IsActive)
            .Select(v => new { v.LookupType!.Key, v.Id })
            .ToListAsync(ct);
        return defaults.ToDictionary(v => v.Key, v => v.Id.ToString(CultureInfo.InvariantCulture));
    }
}
