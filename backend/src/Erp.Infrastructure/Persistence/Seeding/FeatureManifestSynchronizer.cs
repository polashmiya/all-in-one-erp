using Erp.Core.Domain.Metadata;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Persistence.Seeding;

public static class FeatureManifestSynchronizer
{
    public static async Task SyncAsync(ErpDbContext db, FeatureManifestCatalog catalog, ILogger logger, CancellationToken cancellationToken)
    {
        foreach (var manifest in catalog.Manifests)
        {
            var featureId = await db.ErpFeatures.Where(f => f.Code == manifest.Feature).Select(f => f.Id).SingleAsync(cancellationToken);
            var sections = await db.FeatureSections.Where(s => s.FeatureId == featureId).ToListAsync(cancellationToken);
            var fields = await db.FeatureFields.Where(f => f.FeatureId == featureId).ToListAsync(cancellationToken);
            var sectionIds = new Dictionary<string, Guid>();

            foreach (var def in manifest.Sections.OrderBy(s => s.Tab is not null))
            {
                var section = sections.Find(s => s.Key == def.Key);
                if (section is null)
                {
                    section = new FeatureSection { FeatureId = featureId, Key = def.Key, LabelEn = def.Label.En };
                    db.FeatureSections.Add(section);
                }

                section.ParentId = def.Tab is null ? null : sectionIds[def.Tab];
                section.LabelEn = def.Label.En;
                section.LabelBn = def.Label.Bn;
                section.SortOrder = def.Order;
                section.Columns = def.Columns;
                section.IsActive = true;
                sectionIds[def.Key] = section.Id;
            }

            foreach (var def in manifest.Fields)
            {
                var field = fields.Find(f => f.Key == def.Key);
                if (field is null)
                {
                    field = new FeatureField { FeatureId = featureId, Key = def.Key, DataType = def.Type, LabelEn = def.Label.En };
                    db.FeatureFields.Add(field);
                }

                field.SectionId = sectionIds[def.Section];
                field.DataType = def.Type;
                field.LookupSource = def.Lookup;
                field.ParentFieldKey = def.ParentField;
                field.LabelEn = def.Label.En;
                field.LabelBn = def.Label.Bn;
                field.HelpEn = def.Help?.En;
                field.HelpBn = def.Help?.Bn;
                field.PlaceholderEn = def.Placeholder?.En;
                field.PlaceholderBn = def.Placeholder?.Bn;
                field.SortOrder = def.Order;
                field.ColSpan = def.ColSpan;
                field.IsLocked = def.Locked;
                field.IsRequired = def.Required;
                field.MaxLength = def.MaxLength;
                field.MinValue = def.Min;
                field.MaxValue = def.Max;
                field.Precision = def.Precision;
                field.DefaultValue = def.Default;
                field.ListOrder = def.ListOrder;
                field.CanFilter = def.Filter;
                field.CanSort = def.Sort;
                field.IsActive = true;
            }

            foreach (var section in sections.Where(s => !sectionIds.ContainsKey(s.Key))) section.IsActive = false;
            foreach (var field in fields.Where(f => manifest.Fields.All(d => d.Key != f.Key))) field.IsActive = false;

            if (!db.ChangeTracker.HasChanges()) continue;

            await db.SaveChangesAsync(cancellationToken);
            await db.AccountFeatures.AcrossTenants()
                .Where(af => af.FeatureId == featureId)
                .ExecuteUpdateAsync(s => s.SetProperty(af => af.ConfigVersion, af => af.ConfigVersion + 1), cancellationToken);
            logger.LogInformation("Feature manifest: synced {Feature}", manifest.Feature);
        }
    }
}
