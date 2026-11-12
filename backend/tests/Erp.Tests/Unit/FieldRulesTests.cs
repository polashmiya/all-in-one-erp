using System.Text.Json;
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

    private sealed record WithCustom(string? Name, Dictionary<string, JsonElement>? CustomData);

    private static FieldRule Custom(string key, string type, bool visible = true, bool required = false, string? lookup = null) =>
        new(new ConfigField(key, type, new LocalizedLabel(key, null), null, null, required, false, 1, null,
            null, null, null, null, lookup, null, true), visible);

    private static Dictionary<string, JsonElement> Data(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    [Fact]
    public void Custom_values_are_checked_by_type_and_unknown_keys_are_dropped()
    {
        var input = new WithCustom("x", Data("""{"months":"long","note":"ok","grade":9,"since":"2026-13-01","unknown":1}"""));

        var (request, errors) = FieldRules.Apply(input, [
            Custom("months", FieldTypes.Number),
            Custom("note", FieldTypes.Text),
            Custom("grade", FieldTypes.Lookup, lookup: "grade"),
            Custom("since", FieldTypes.Date),
            Custom("color", FieldTypes.Text, required: true),
        ], null, new Dictionary<string, HashSet<int>> { ["grade"] = [3] });

        Assert.Equal(["customData.color", "customData.grade", "customData.months", "customData.since"], errors.Keys.Order());
        Assert.DoesNotContain("unknown", request.CustomData!.Keys);
        Assert.Equal("ok", request.CustomData["note"].GetString());
    }

    [Fact]
    public void Custom_values_of_hidden_and_removed_fields_are_kept_on_update()
    {
        var saved = new { name = "x", customData = new { secret = "s", old = 5, grade = 9 } };
        var input = new WithCustom("x", Data("""{"secret":"changed","grade":9,"note":"n"}"""));

        var (request, errors) = FieldRules.Apply(input, [
            Custom("secret", FieldTypes.Text, visible: false),
            Custom("grade", FieldTypes.Lookup, lookup: "grade"),
            Custom("note", FieldTypes.Text),
        ], saved, new Dictionary<string, HashSet<int>> { ["grade"] = [3] });

        Assert.Empty(errors);
        Assert.Equal("""{"old":5,"secret":"s","grade":9,"note":"n"}""", JsonSerializer.Serialize(request.CustomData));
    }

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
