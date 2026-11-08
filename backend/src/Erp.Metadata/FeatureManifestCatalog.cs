using System.Text.RegularExpressions;
using Erp.Core.Modules;

namespace Erp.Metadata;

public sealed partial class FeatureManifestCatalog
{
    public FeatureManifestCatalog(IEnumerable<IFeatureManifestProvider> providers, ErpMenuCatalog menu)
    {
        Manifests = providers.SelectMany(p => p.GetManifests()).ToList();

        var features = menu.Features.Select(f => f.Code).ToHashSet();
        Ensure(Manifests.Select(m => m.Feature), "feature manifest", "catalog");
        foreach (var manifest in Manifests) Validate(manifest, features);
    }

    public IReadOnlyList<FeatureManifest> Manifests { get; }

    private static void Validate(FeatureManifest manifest, HashSet<string> features)
    {
        var name = manifest.Feature;
        if (!features.Contains(name)) throw Invalid(name, "no ERP feature has this code");

        Ensure(manifest.Sections.Select(s => s.Key), "section", name);
        Ensure(manifest.Fields.Select(f => f.Key), "field", name);

        var tabs = manifest.Sections.Where(s => s.Tab is null).Select(s => s.Key).ToHashSet();
        var sections = manifest.Sections.Where(s => s.Tab is not null).ToDictionary(s => s.Key);
        foreach (var section in sections.Values)
        {
            if (!tabs.Contains(section.Tab!)) throw Invalid(name, $"section '{section.Key}' points to unknown tab '{section.Tab}'");
            if (section.Columns is < 1 or > 4) throw Invalid(name, $"section '{section.Key}' needs 1 to 4 columns");
        }

        var keys = manifest.Fields.Select(f => f.Key).ToHashSet();
        foreach (var field in manifest.Fields)
        {
            if (!FieldKey().IsMatch(field.Key)) throw Invalid(name, $"field key '{field.Key}' must be camelCase");
            if (!FieldTypes.All.Contains(field.Type)) throw Invalid(name, $"field '{field.Key}' has unknown type '{field.Type}'");
            if (!sections.ContainsKey(field.Section)) throw Invalid(name, $"field '{field.Key}' points to unknown section '{field.Section}'");
            if (field.ColSpan is < 1 or > 4) throw Invalid(name, $"field '{field.Key}' needs a column span of 1 to 4");
            if ((field.Type == FieldTypes.Lookup) != (field.Lookup is not null))
                throw Invalid(name, $"field '{field.Key}': only lookup fields have a lookup source, and they need one");
            if (field.ParentField is not null && !keys.Contains(field.ParentField))
                throw Invalid(name, $"field '{field.Key}' points to unknown parent field '{field.ParentField}'");
        }
    }

    private static void Ensure(IEnumerable<string> keys, string what, string owner)
    {
        var duplicate = keys.GroupBy(k => k).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw Invalid(owner, $"duplicate {what} key '{duplicate.Key}'");
    }

    private static InvalidOperationException Invalid(string owner, string problem) =>
        new($"Feature manifest '{owner}': {problem}.");

    [GeneratedRegex("^[a-z][a-zA-Z0-9]*$")]
    private static partial Regex FieldKey();
}
