using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Locations;

public sealed record LocationOption(int Id, string Name, string NameBn, int? ParentId, bool IsActive);

public static class LocationEndpoints
{
    public const string District = "district";
    public const string Thana = "thana";

    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder erp)
    {
        var locations = erp.MapGroup("/locations").WithTags("Locations");

        locations.MapGet("/districts", Districts)
            .WithName("GetDistricts")
            .WithSummary("Every district of Bangladesh");

        locations.MapGet("/thanas", Thanas)
            .WithName("GetThanas")
            .WithSummary("Every thana of Bangladesh, each with its district as parent");

        return erp;
    }

    private static Task<List<LocationOption>> Districts(ErpDbContext db, CancellationToken ct) =>
        db.Districts.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new LocationOption(d.Id, d.Name, d.NameBn, null, true))
            .ToListAsync(ct);

    private static Task<List<LocationOption>> Thanas(ErpDbContext db, CancellationToken ct) =>
        db.Thanas.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new LocationOption(t.Id, t.Name, t.NameBn, t.DistrictId, true))
            .ToListAsync(ct);
}
