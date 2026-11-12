using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Erp.Metadata;

public static class FieldRules
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static (T Request, Dictionary<string, string[]> Errors) Apply<T>(
        T request,
        IReadOnlyList<FieldRule> rules,
        object? existing,
        IReadOnlyDictionary<string, HashSet<int>>? lookupValues = null)
    {
        var values = JsonSerializer.SerializeToNode(request, Json)!.AsObject();
        var current = existing is null ? null : JsonSerializer.SerializeToNode(existing, Json)!.AsObject();
        var saved = current?[CustomFieldData.Property] as JsonObject;
        var custom = CustomValues(values[CustomFieldData.Property] as JsonObject, saved, rules);
        var errors = new Dictionary<string, string[]>();

        foreach (var (field, visible) in rules)
        {
            if (!field.Custom && !values.ContainsKey(field.Key)) continue;
            var (target, source) = field.Custom ? (custom, saved) : (values, current);

            if (!visible || field.ReadOnly)
                target[field.Key] = current is null ? DefaultOf(field) : source?[field.Key]?.DeepClone();
            else if ((CustomProblem(field, target[field.Key], source?[field.Key], lookupValues) ?? Problem(field, target[field.Key])) is { } problem)
                errors[field.Custom ? $"{CustomFieldData.Property}.{field.Key}" : field.Key] = [problem];
        }

        if (values.ContainsKey(CustomFieldData.Property)) values[CustomFieldData.Property] = custom;
        return (values.Deserialize<T>(Json)!, errors);
    }

    private static JsonObject CustomValues(JsonObject? sent, JsonObject? saved, IReadOnlyList<FieldRule> rules)
    {
        var keys = rules.Where(r => r.Field.Custom).Select(r => r.Field.Key).ToHashSet();
        var custom = new JsonObject();
        foreach (var (key, value) in saved ?? [])
            if (!keys.Contains(key)) custom[key] = value?.DeepClone();
        foreach (var key in keys) custom[key] = sent?[key]?.DeepClone();
        return custom;
    }

    private static string? CustomProblem(
        ConfigField field, JsonNode? value, JsonNode? saved, IReadOnlyDictionary<string, HashSet<int>>? lookupValues)
    {
        if (!field.Custom || IsEmpty(field, value)) return null;
        if (!HasType(field, value!)) return "This value has the wrong type.";
        if (field.Type != FieldTypes.Lookup || JsonNode.DeepEquals(value, saved)) return null;

        var active = lookupValues?.GetValueOrDefault(field.Lookup ?? "");
        return active?.Contains((int)value!.GetValue<decimal>()) == true ? null : "Choose an active value from the list.";
    }

    private static bool HasType(ConfigField field, JsonNode value)
    {
        var kind = value.GetValueKind();
        return field.Type switch
        {
            FieldTypes.Boolean => kind is JsonValueKind.True or JsonValueKind.False,
            FieldTypes.Text or FieldTypes.TextArea => kind == JsonValueKind.String,
            FieldTypes.Date => kind == JsonValueKind.String &&
                DateOnly.TryParseExact(value.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            FieldTypes.Number or FieldTypes.Lookup => kind == JsonValueKind.Number && value.GetValue<decimal>() % 1 == 0,
            _ => kind == JsonValueKind.Number,
        };
    }

    private static JsonNode? DefaultOf(ConfigField field)
    {
        if (field.DefaultValue is not { } text) return field.Type == FieldTypes.Boolean ? JsonValue.Create(false) : null;
        if (field.Type == FieldTypes.Boolean) return JsonValue.Create(bool.Parse(text));
        return IsNumeric(field) && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? JsonValue.Create(number)
            : JsonValue.Create(text);
    }

    private static string? Problem(ConfigField field, JsonNode? value)
    {
        if (field.Type == FieldTypes.Boolean) return null;
        if (IsEmpty(field, value)) return field.Required ? $"{field.Label.En} is required." : null;

        if (value!.GetValueKind() == JsonValueKind.String)
        {
            var text = value.GetValue<string>().Trim();
            return field.MaxLength is { } max && text.Length > max ? $"Use at most {max} characters." : null;
        }

        if (value.GetValueKind() != JsonValueKind.Number) return null;
        var number = value.GetValue<decimal>();
        if (field.Min is { } min && number < min) return $"Use {Format(min)} or more.";
        if (field.Max is { } limit && number > limit) return $"Use {Format(limit)} or less.";
        return null;
    }

    private static bool IsEmpty(ConfigField field, JsonNode? value) => value?.GetValueKind() switch
    {
        null or JsonValueKind.Null => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetValue<string>()),
        JsonValueKind.Number => field.Type == FieldTypes.Lookup && value.GetValue<decimal>() == 0,
        _ => false,
    };

    private static bool IsNumeric(ConfigField field) =>
        field.Type is FieldTypes.Number or FieldTypes.Decimal or FieldTypes.Money or FieldTypes.Percent or FieldTypes.Lookup;

    private static string Format(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
