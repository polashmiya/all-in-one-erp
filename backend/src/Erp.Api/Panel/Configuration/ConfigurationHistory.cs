using System.Text.Json;
using System.Text.Json.Serialization;
using Erp.Core.Domain.Metadata;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Configuration;

public sealed record ConfigurationChangeRow(
    Guid Id,
    DateTimeOffset At,
    string? UserName,
    string? Feature,
    Guid? BusinessUnitId,
    string? BusinessUnitName,
    string Area,
    string Key,
    JsonElement? Before,
    JsonElement? After);

public sealed record ConfigurationChangeList(IReadOnlyList<ConfigurationChangeRow> Items, int Total);

public static class ConfigurationHistory
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Record(
        ErpDbContext db, Guid accountId, Guid? businessUnitId, string? feature, string area, string key, object? before, object? after)
    {
        var (from, to) = (Text(before), Text(after));
        if (from == to) return;

        db.ConfigurationChanges.Add(new ConfigurationChange
        {
            AccountId = accountId,
            BusinessUnitId = businessUnitId,
            Feature = feature,
            Area = area,
            Key = key,
            Before = from,
            After = to,
        });
    }

    public static IEndpointRouteBuilder MapConfigurationHistoryEndpoints(this IEndpointRouteBuilder panel)
    {
        panel.MapGet("/accounts/{id:guid}/history", List)
            .WithName("ListConfigurationChanges")
            .WithTags("PanelFeatureConfiguration")
            .WithSummary("Who changed which screen settings, layouts, custom fields and lookup values of the account, newest first");

        return panel;
    }

    private static async Task<Results<Ok<ConfigurationChangeList>, NotFound>> List(
        Guid id, string? feature, Guid? businessUnitId, string? area, int? page, int? pageSize, ErpDbContext db, CancellationToken ct)
    {
        if (!await db.Accounts.AcrossTenants().AnyAsync(a => a.Id == id, ct)) return TypedResults.NotFound();

        var changes = db.ConfigurationChanges.AcrossTenants().AsNoTracking().Where(c => c.AccountId == id);
        if (!string.IsNullOrWhiteSpace(feature)) changes = changes.Where(c => c.Feature == feature);
        if (businessUnitId is not null) changes = changes.Where(c => c.BusinessUnitId == businessUnitId);
        if (!string.IsNullOrWhiteSpace(area)) changes = changes.Where(c => c.Area == area);

        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var total = await changes.CountAsync(ct);
        var rows = await changes
            .OrderByDescending(c => c.CreatedAt)
            .Skip((Math.Max(1, page ?? 1) - 1) * size)
            .Take(size)
            .Select(c => new
            {
                c.Id,
                c.CreatedAt,
                UserName = db.PanelUsers.Where(u => u.Id == c.CreatedBy).Select(u => u.FullName).FirstOrDefault(),
                c.Feature,
                c.BusinessUnitId,
                BusinessUnitName = db.BusinessUnits.AcrossTenants().Where(b => b.Id == c.BusinessUnitId).Select(b => b.Name).FirstOrDefault(),
                c.Area,
                c.Key,
                c.Before,
                c.After,
            })
            .ToListAsync(ct);

        return TypedResults.Ok(new ConfigurationChangeList(
            rows.Select(r => new ConfigurationChangeRow(
                r.Id, r.CreatedAt, r.UserName, r.Feature, r.BusinessUnitId, r.BusinessUnitName, r.Area, r.Key,
                Element(r.Before), Element(r.After))).ToList(),
            total));
    }

    private static string? Text(object? value) => value is null ? null : JsonSerializer.Serialize(value, Json);

    private static JsonElement? Element(string? json) => json is null ? null : JsonSerializer.Deserialize<JsonElement>(json);
}
