using System.Text.Json;

namespace Erp.Metadata;

public static class CustomFieldData
{
    public const string Property = "customData";

    public static Dictionary<string, JsonElement>? Parse(string? json) =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

    public static string? ToJson(Dictionary<string, JsonElement>? values)
    {
        var filled = values?.Where(v => v.Value.ValueKind != JsonValueKind.Null).ToDictionary(v => v.Key, v => v.Value);
        return filled is { Count: > 0 } ? JsonSerializer.Serialize(filled) : null;
    }
}
