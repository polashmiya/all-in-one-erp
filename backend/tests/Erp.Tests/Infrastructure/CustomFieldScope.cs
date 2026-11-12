using Erp.Core.Domain.Metadata;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Infrastructure;

public sealed class CustomFieldScope(Guid accountId, Guid featureId) : IAsyncDisposable
{
    public static async Task<CustomFieldScope> ApplyAsync(
        Guid businessUnitId, string feature, params (string Key, string Type, string? Lookup)[] fields)
    {
        await using var db = ApiFactory.CreateDbContext();
        var accountId = await db.BusinessUnits.AcrossTenants().Where(b => b.Id == businessUnitId).Select(b => b.AccountId).SingleAsync();
        var featureId = await db.ErpFeatures.Where(f => f.Code == feature).Select(f => f.Id).SingleAsync();
        var sectionId = await db.FeatureFields.Where(f => f.FeatureId == featureId).Select(f => f.SectionId).FirstAsync();

        db.FeatureFields.AddRange(fields.Select(f => new FeatureField
        {
            FeatureId = featureId,
            AccountId = accountId,
            SectionId = sectionId,
            Key = f.Key,
            DataType = f.Type,
            LookupSource = f.Lookup,
            LabelEn = f.Key,
        }));
        await db.SaveChangesAsync();
        await BumpAsync(db, accountId, featureId);
        return new CustomFieldScope(accountId, featureId);
    }

    public async Task DeactivateAsync(string key)
    {
        await using var db = ApiFactory.CreateDbContext();
        await db.FeatureFields
            .Where(f => f.AccountId == accountId && f.FeatureId == featureId && f.Key == key)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsActive, false));
        await BumpAsync(db, accountId, featureId);
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = ApiFactory.CreateDbContext();
        await db.FeatureFields.Where(f => f.AccountId == accountId && f.FeatureId == featureId).ExecuteDeleteAsync();
        await BumpAsync(db, accountId, featureId);
    }

    private static Task BumpAsync(ErpDbContext db, Guid accountId, Guid featureId) =>
        db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == accountId && af.FeatureId == featureId)
            .ExecuteUpdateAsync(s => s.SetProperty(af => af.ConfigVersion, af => af.ConfigVersion + 1));
}
