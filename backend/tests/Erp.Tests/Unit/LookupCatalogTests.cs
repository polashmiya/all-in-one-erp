using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement.Items;

namespace Erp.Tests.Unit;

public sealed class LookupCatalogTests
{
    private sealed class Provider(params LookupDef[] lookups) : ILookupProvider
    {
        public IEnumerable<LookupDef> GetLookups() => lookups;
    }

    [Fact]
    public void Item_lookups_are_valid_with_one_default_supply_type()
    {
        var catalog = new LookupCatalog([new ItemLookups()]);

        Assert.Equal([ItemLookups.ItemType, ItemLookups.SupplyType], catalog.Lookups.Select(l => l.Key));
        Assert.Equal("STD", Assert.Single(catalog.Lookups[1].Defaults, v => v.IsDefault).Code);
    }

    [Theory]
    [InlineData("Bad_Key", "a", "b", false, "lower-case")]
    [InlineData("colours", "red", "RED", false, "Duplicate value code")]
    [InlineData("colours", "red", "blue", true, "more than one default")]
    public void Catalog_rejects_invalid_lookups(string key, string first, string second, bool twoDefaults, string problem)
    {
        var lookup = new LookupDef(key, new("Colours", "রং"),
            [new(first, new(first, first), IsDefault: true), new(second, new(second, second), IsDefault: twoDefaults)]);

        var error = Assert.Throws<InvalidOperationException>(() => new LookupCatalog([new Provider(lookup)]));

        Assert.Contains(problem, error.Message);
    }
}
