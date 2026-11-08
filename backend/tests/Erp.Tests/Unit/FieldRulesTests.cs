using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Tests.Unit;

public sealed class FieldRulesTests
{
    private sealed record Sample(string? Name, int? TypeId, bool Flag, decimal? Price, string? Note);

    private static FieldRule Rule(string key, string type, bool visible = true, bool required = false, string? defaultValue = null,
        int? maxLength = null, decimal? min = null, decimal? max = null, bool readOnly = false) =>
        new(new ConfigField(key, type, new LocalizedLabel(key, null), null, null, required, readOnly, 1, defaultValue,
            maxLength, min, max, null, null, null), visible);

    private static readonly Sample Input = new("  ", 0, true, 5m, "client");

    [Fact]
    public void Visible_fields_are_checked_for_required_length_and_range()
    {
        var (_, errors) = FieldRules.Apply(Input with { Note = "too long" }, [
            Rule("name", FieldTypes.Text, required: true),
            Rule("typeId", FieldTypes.Lookup, required: true),
            Rule("price", FieldTypes.Money, min: 10m),
            Rule("note", FieldTypes.Text, maxLength: 3),
            Rule("flag", FieldTypes.Boolean, required: true),
        ], null);

        Assert.Equal(["name", "note", "price", "typeId"], errors.Keys.Order());
        Assert.Equal("name is required.", errors["name"][0]);
        Assert.Equal("Use 10 or more.", errors["price"][0]);
        Assert.Equal("Use at most 3 characters.", errors["note"][0]);
    }

    [Fact]
    public void Hidden_and_read_only_fields_take_defaults_on_create()
    {
        var (request, errors) = FieldRules.Apply(Input, [
            Rule("name", FieldTypes.Text, visible: false, required: true),
            Rule("typeId", FieldTypes.Lookup, visible: false, defaultValue: "3"),
            Rule("flag", FieldTypes.Boolean, visible: false),
            Rule("price", FieldTypes.Money, readOnly: true, defaultValue: "1.5"),
            Rule("note", FieldTypes.Text, visible: false, defaultValue: "fixed"),
        ], null);

        Assert.Empty(errors);
        Assert.Equal(new Sample(null, 3, false, 1.5m, "fixed"), request);
    }

    [Fact]
    public void Hidden_and_read_only_fields_keep_their_saved_values_on_update()
    {
        var saved = new { name = "Saved", typeId = 7, flag = false, price = 9m, note = "saved", other = "ignored" };

        var (request, errors) = FieldRules.Apply(Input with { Name = "New" }, [
            Rule("typeId", FieldTypes.Lookup, visible: false, required: true),
            Rule("flag", FieldTypes.Boolean, readOnly: true),
            Rule("note", FieldTypes.Text, visible: false),
            Rule("missing", FieldTypes.Text, required: true),
        ], saved);

        Assert.Empty(errors);
        Assert.Equal(new Sample("New", 7, false, 5m, "saved"), request);
    }
}
