using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Lookups;

public sealed record LookupOption(int Id, string? Code, string Name, string? NameBn, bool IsActive, bool IsDefault);

public static class LookupEndpoints
{
    public static IEndpointRouteBuilder MapLookupEndpoints(this IEndpointRouteBuilder erp)
    {
        erp.MapGet("/lookups/{type}", List)
            .WithName("GetLookupValues")
            .WithTags("Lookups")
            .WithSummary("Values of an account's lookup list, inactive ones included")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return erp;
    }

    private static async Task<Results<Ok<List<LookupOption>>, NotFound>> List(string type, ErpDbContext db, CancellationToken ct)
    {
        if (!await db.LookupTypes.AnyAsync(t => t.Key == type && t.IsActive, ct)) return TypedResults.NotFound();

        return TypedResults.Ok(await db.LookupValues.AsNoTracking()
            .Where(v => v.LookupType!.Key == type)
            .OrderBy(v => v.SortOrder).ThenBy(v => v.NameEn)
            .Select(v => new LookupOption(v.Id, v.Code, v.NameEn, v.NameBn, v.IsActive, v.IsDefault))
            .ToListAsync(ct));
    }
}
