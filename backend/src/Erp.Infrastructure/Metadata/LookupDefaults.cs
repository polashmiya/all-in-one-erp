using Erp.Core.Domain.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Metadata;

public sealed class LookupDefaults(ErpDbContext db, LookupCatalog catalog)
{
    public async Task SyncTypesAsync(CancellationToken ct)
    {
        var types = await db.LookupTypes.ToListAsync(ct);
        foreach (var lookup in catalog.Lookups)
        {
            var type = types.Find(t => t.Key == lookup.Key);
            if (type is null)
            {
                type = new LookupType { Key = lookup.Key, NameEn = lookup.Name.En };
                db.LookupTypes.Add(type);
            }

            type.NameEn = lookup.Name.En;
            type.NameBn = lookup.Name.Bn;
            type.IsActive = true;
        }

        foreach (var type in types.Where(t => catalog.Lookups.All(l => l.Key != t.Key))) type.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task AddMissingAsync(Guid accountId, CancellationToken ct)
    {
        var types = await db.LookupTypes.Where(t => t.IsActive).ToDictionaryAsync(t => t.Key, t => t.Id, ct);
        var seeded = await db.LookupValues.AcrossTenants()
            .Where(v => v.AccountId == accountId)
            .Select(v => v.LookupTypeId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var lookup in catalog.Lookups)
        {
            if (!types.TryGetValue(lookup.Key, out var typeId) || seeded.Contains(typeId)) continue;

            db.LookupValues.AddRange(lookup.Defaults.Select((value, index) => new LookupValue
            {
                AccountId = accountId,
                LookupTypeId = typeId,
                Code = value.Code,
                NameEn = value.Name.En,
                NameBn = value.Name.Bn,
                SortOrder = (index + 1) * 10,
                IsDefault = value.IsDefault,
            }));
        }
    }
}
