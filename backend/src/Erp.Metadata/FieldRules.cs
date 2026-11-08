using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Erp.Metadata;

public static class FieldRules
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static (T Request, Dictionary<string, string[]> Errors) Apply<T>(T request, IReadOnlyList<FieldRule> rules, object? existing)
    {
        var values = JsonSerializer.SerializeToNode(request, Json)!.AsObject();
        var current = existing is null ? null : JsonSerializer.SerializeToNode(existing, Json)!.AsObject();
        var errors = new Dictionary<string, string[]>();

        foreach (var (field, visible) in rules)
        {
            if (!values.ContainsKey(field.Key)) continue;

            if (!visible || field.ReadOnly)
                values[field.Key] = current is null ? DefaultOf(field) : current[field.Key]?.DeepClone();
            else if (Problem(field, values[field.Key]) is { } problem)
                errors[field.Key] = [problem];
        }

        return (values.Deserialize<T>(Json)!, errors);
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
