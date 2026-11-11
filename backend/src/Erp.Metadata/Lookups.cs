using System.Text.RegularExpressions;
using Erp.Core.Modules;

namespace Erp.Metadata;

public sealed record LookupValueDef(string Code, MenuText Name, bool IsDefault = false);

public sealed record LookupDef(string Key, MenuText Name, IReadOnlyList<LookupValueDef> Defaults);

public interface ILookupProvider
{
    IEnumerable<LookupDef> GetLookups();
}

public sealed partial class LookupCatalog
{
    public LookupCatalog(IEnumerable<ILookupProvider> providers)
    {
        Lookups = providers.SelectMany(p => p.GetLookups()).ToList();

        Ensure(Lookups.Select(l => l.Key), "lookup");
        foreach (var lookup in Lookups)
        {
            if (!LookupKey().IsMatch(lookup.Key))
                throw new InvalidOperationException($"Lookup key '{lookup.Key}' must be lower-case letters, digits and dashes.");
            Ensure(lookup.Defaults.Select(v => v.Code), $"value code in lookup '{lookup.Key}'");
            if (lookup.Defaults.Count(v => v.IsDefault) > 1)
                throw new InvalidOperationException($"Lookup '{lookup.Key}' has more than one default value.");
        }
    }

    public IReadOnlyList<LookupDef> Lookups { get; }

    private static void Ensure(IEnumerable<string> keys, string what)
    {
        var duplicate = keys.GroupBy(k => k, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate {what} '{duplicate.Key}'.");
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex LookupKey();
}
