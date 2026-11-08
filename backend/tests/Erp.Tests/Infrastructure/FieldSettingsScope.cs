using Erp.Core.Domain.Metadata;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Infrastructure;

public sealed class FieldSettingsScope(Guid accountId, Guid featureId) : IAsyncDisposable
{
    public static async Task<FieldSettingsScope> ApplyAsync(
        Guid businessUnitId, string feature, params (string Field, Action<AccountFieldSetting> Set)[] settings)
    {
        await using var db = ApiFactory.CreateDbContext();
        var accountId = await db.BusinessUnits.AcrossTenants().Where(b => b.Id == businessUnitId).Select(b => b.AccountId).SingleAsync();
        var featureId = await db.ErpFeatures.Where(f => f.Code == feature).Select(f => f.Id).SingleAsync();
        var fieldIds = await db.FeatureFields.Where(f => f.FeatureId == featureId).ToDictionaryAsync(f => f.Key, f => f.Id);

        db.AccountFieldSettings.AddRange(settings.Select(s =>
        {
            var row = new AccountFieldSetting { AccountId = accountId, FieldId = fieldIds[s.Field] };
            s.Set(row);
            return row;
        }));
        await db.SaveChangesAsync();
        await BumpAsync(db, accountId, featureId);
        return new FieldSettingsScope(accountId, featureId);
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = ApiFactory.CreateDbContext();
        await db.AccountFieldSettings.AcrossTenants()
            .Where(s => s.AccountId == accountId && s.Field!.FeatureId == featureId)
            .ExecuteDeleteAsync();
        await BumpAsync(db, accountId, featureId);
    }

    private static Task BumpAsync(ErpDbContext db, Guid accountId, Guid featureId) =>
        db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == accountId && af.FeatureId == featureId)
            .ExecuteUpdateAsync(s => s.SetProperty(af => af.ConfigVersion, af => af.ConfigVersion + 1));
}
