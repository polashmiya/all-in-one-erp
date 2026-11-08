using Erp.Core.Domain.Metadata;
using Erp.Core.Modules;
using Erp.Metadata;
using Erp.Modules.Inventory;
using Erp.Modules.Inventory.ItemManagement.Items;

namespace Erp.Tests.Unit;

public sealed class FeatureConfigTests
{
    private sealed class Provider(params FeatureManifest[] manifests) : IFeatureManifestProvider
    {
        public IEnumerable<FeatureManifest> GetManifests() => manifests;
    }

    private static readonly ErpMenuCatalog Menu = new([new ItemManagementMenu()]);

    private static FeatureManifest Manifest(params FieldDef[] fields) =>
        new(ItemManagementMenu.Uom, [new("main", new("Main", "মূল"), 1), new("info", new("Info", "তথ্য"), 1, "main")], fields);

    private static readonly FeatureSection Main = Section("main", 10, null);
    private static readonly FeatureSection Other = Section("other", 20, null);
    private static readonly FeatureSection Basic = Section("basic", 10, Main.Id);
    private static readonly FeatureSection Extra = Section("extra", 20, Main.Id);
    private static readonly FeatureSection Notes = Section("notes", 10, Other.Id);
    private static readonly FeatureSection[] Sections = [Main, Other, Basic, Extra, Notes];

    private static readonly FeatureField Name = Field("name", Basic, 10, f => { f.IsLocked = true; f.ListOrder = 1; });
    private static readonly FeatureField Code = Field("code", Basic, 20, f => { f.MaxLength = 50; f.ListOrder = 2; f.CanSort = true; });
    private static readonly FeatureField Type = Field("typeId", Extra, 10, f => { f.IsRequired = true; f.CanFilter = true; });
    private static readonly FeatureField Remark = Field("remark", Notes, 10, f => f.MinValue = 0m);
    private static readonly FeatureField[] Fields = [Name, Code, Type, Remark];

    private static FeatureSection Section(string key, int order, Guid? parentId) =>
        new() { Id = Guid.NewGuid(), Key = key, LabelEn = key, LabelBn = null, SortOrder = order, Columns = 4, ParentId = parentId };

    private static FeatureField Field(string key, FeatureSection section, int order, Action<FeatureField> set)
    {
        var field = new FeatureField { Id = Guid.NewGuid(), Key = key, DataType = FieldTypes.Text, LabelEn = key, SectionId = section.Id, SortOrder = order };
        set(field);
        return field;
    }

    private static FeatureConfig Build(AccountFieldSetting[]? fields = null, AccountSectionSetting[]? sections = null) =>
        FeatureConfigBuilder.Build("x.y", 3, Sections, Fields, sections ?? [], fields ?? []).Config;

    private static IEnumerable<ConfigField> AllFields(FeatureConfig config) =>
        config.Tabs.SelectMany(t => t.Sections).SelectMany(s => s.Fields);

    [Fact]
    public void Item_profile_manifest_is_valid()
    {
        var catalog = new FeatureManifestCatalog([new ItemProfileManifest()], Menu);

        var manifest = Assert.Single(catalog.Manifests);
        Assert.Equal(ItemManagementMenu.ItemProfile, manifest.Feature);
        Assert.Equal(["name", "minorCategoryId", "uomId"], manifest.Fields.Where(f => f.Locked).Select(f => f.Key));
    }

    [Theory]
    [InlineData("unknown.feature", "main", FieldTypes.Text, null, "no ERP feature")]
    [InlineData(ItemManagementMenu.Uom, "missing", FieldTypes.Text, null, "unknown section")]
    [InlineData(ItemManagementMenu.Uom, "main", FieldTypes.Text, null, "unknown section")]
    [InlineData(ItemManagementMenu.Uom, "info", FieldTypes.Lookup, null, "lookup source")]
    [InlineData(ItemManagementMenu.Uom, "info", FieldTypes.Text, "uom", "lookup source")]
    [InlineData(ItemManagementMenu.Uom, "info", "colour", null, "unknown type")]
    public void Catalog_rejects_invalid_manifests(string feature, string section, string type, string? lookup, string problem)
    {
        var manifest = Manifest(new FieldDef("name", type, new("Name", "নাম"), section, 1) { Lookup = lookup }) with { Feature = feature };

        var error = Assert.Throws<InvalidOperationException>(() => new FeatureManifestCatalog([new Provider(manifest)], Menu));

        Assert.Contains(problem, error.Message);
    }

    [Fact]
    public void Catalog_rejects_duplicate_and_badly_named_fields()
    {
        var duplicate = Manifest(new FieldDef("name", FieldTypes.Text, new("A", "A"), "info", 1), new FieldDef("name", FieldTypes.Text, new("B", "B"), "info", 2));
        var badKey = Manifest(new FieldDef("Item_Name", FieldTypes.Text, new("A", "A"), "info", 1));

        Assert.Contains("duplicate field", Assert.Throws<InvalidOperationException>(() => new FeatureManifestCatalog([new Provider(duplicate)], Menu)).Message);
        Assert.Contains("camelCase", Assert.Throws<InvalidOperationException>(() => new FeatureManifestCatalog([new Provider(badKey)], Menu)).Message);
    }

    [Fact]
    public void Defaults_give_tabs_sections_columns_and_filters_in_order()
    {
        var config = Build();

        Assert.Equal(("x.y", 3), (config.Feature, config.Version));
        Assert.Equal(["main", "other"], config.Tabs.Select(t => t.Key));
        Assert.Equal(["basic", "extra"], config.Tabs[0].Sections.Select(s => s.Key));
        Assert.Equal(["name", "code", "typeId", "remark"], AllFields(config).Select(f => f.Key));
        Assert.Equal([true, false, true, false], AllFields(config).Select(f => f.Required));
        Assert.Equal([new ConfigColumn("name", false), new ConfigColumn("code", true)], config.Columns);
        Assert.Equal(["typeId"], config.Filters);
    }

    [Fact]
    public void Account_settings_override_the_defaults()
    {
        var config = Build(
        [
            new() { FieldId = Code.Id, IsRequired = true, LabelBn = "কোড", MaxLength = 80, SortOrder = 5, InList = false },
            new() { FieldId = Type.Id, IsVisible = false },
            new() { FieldId = Remark.Id, SectionId = Basic.Id, IsReadOnly = true, MinValue = -5m, InList = true, ListOrder = 0 },
        ],
        [new() { SectionId = Other.Id, LabelEn = "More", SortOrder = 1 }]);

        Assert.Equal(["main"], config.Tabs.Select(t => t.Key));
        var basic = Assert.Single(config.Tabs[0].Sections);
        Assert.Equal(["code", "name", "remark"], basic.Fields.Select(f => f.Key));

        var code = basic.Fields[0];
        Assert.Equal((true, "code", "কোড", 50), (code.Required, code.Label.En, code.Label.Bn, code.MaxLength));
        var remark = basic.Fields[2];
        Assert.Equal((true, 0m), (remark.ReadOnly, remark.Min));
        Assert.Equal(["remark", "name"], config.Columns.Select(c => c.Key));
        Assert.Empty(config.Filters);
    }

    [Fact]
    public void Locked_fields_cannot_be_hidden_made_optional_or_read_only()
    {
        var config = Build(
            [new() { FieldId = Name.Id, IsVisible = false, IsRequired = false, IsReadOnly = true }],
            [new() { SectionId = Main.Id, IsVisible = false }, new() { SectionId = Other.Id, IsVisible = false }]);

        var tab = Assert.Single(config.Tabs);
        var name = Assert.Single(Assert.Single(tab.Sections).Fields);
        Assert.Equal(("name", true, false), (name.Key, name.Required, name.ReadOnly));
    }

    [Fact]
    public void Inactive_fields_are_left_out_with_their_empty_tabs()
    {
        var remark = Field("remark", Notes, 10, f => f.IsActive = false);

        var config = FeatureConfigBuilder.Build("x.y", 3, Sections, [Name, Code, Type, remark], [], []).Config;

        Assert.Equal(["main"], config.Tabs.Select(t => t.Key));
    }
}
