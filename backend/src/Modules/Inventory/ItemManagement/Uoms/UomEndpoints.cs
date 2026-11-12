using Erp.Core.Authorization;
using Erp.Core.Domain.Inventory;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Inventory.ItemManagement.Uoms;

internal static class UomEndpoints
{
    private const string Feature = ItemManagementMenu.Uom;

    public static void Map(IEndpointRouteBuilder group)
    {
        var uoms = group.MapGroup("/uoms").WithTags("UnitsOfMeasure");

        uoms.MapGet("", List)
            .WithName("ListUoms")
            .WithSummary("Units of measure of the account (shared by all its business units) with status counts")
            .RequirePermission($"{Feature}.view");

        uoms.MapGet("/options", Options)
            .WithName("GetUomOptions")
            .WithSummary("Every unit of measure of the account, for dropdowns")
            .RequireAnyPermission(ItemManagementMenu.LookupReaders);

        uoms.MapGet("/{id:int}", Get)
            .WithName("GetUom")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        uoms.MapPost("", Create)
            .WithName("CreateUom")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveUomRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        uoms.MapPut("/{id:int}", Update)
            .WithName("UpdateUom")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveUomRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        uoms.MapPost("/status", SetStatus)
            .WithName("SetUomsStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<RecordStatusRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        uoms.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUom")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        uoms.MapPost("/delete", DeleteMany)
            .WithName("DeleteUoms")
            .RequirePermission($"{Feature}.delete")
            .Validate<RecordIdsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static IQueryable<UomRow> Rows(IQueryable<UnitOfMeasure> query) =>
        query.Select(u => new UomRow(
            u.Id, u.Code, u.Name, u.Description, u.IsDecimalAllowed, u.RoundingLength, u.IsActive, u.CreatedAt,
            u.ModifiedAt,
            CustomFieldData.Parse(u.CustomData)));

    private static async Task<UomList> List(
        string? search, string? status, string? sort, int? page, int? pageSize, bool? decimalAllowed, ErpDbContext db, CancellationToken ct)
    {
        var query = db.UnitsOfMeasure.AsNoTracking();
        if (decimalAllowed is not null) query = query.Where(u => u.IsDecimalAllowed == decimalAllowed);
        if (MasterData.Search(search) is { } term)
            query = query.Where(u => u.Code.Contains(term) || u.Name.Contains(term) || (u.Description != null && u.Description.Contains(term)));

        var counts = await query.CountByStatusAsync(ct);
        query = query.WithStatus(status);

        var sorted = sort switch
        {
            "-code" => query.OrderByDescending(u => u.Code),
            "name" => query.OrderBy(u => u.Name),
            "-name" => query.OrderByDescending(u => u.Name),
            "modifiedAt" => query.OrderBy(u => u.ModifiedAt ?? u.CreatedAt),
            "-modifiedAt" => query.OrderByDescending(u => u.ModifiedAt ?? u.CreatedAt),
            _ => query.OrderBy(u => u.Code),
        };

        var items = await Rows(sorted.ThenBy(u => u.Id).Page(page, pageSize)).ToListAsync(ct);
        return new UomList(items, counts.TotalFor(status), counts);
    }

    private static Task<List<ItemLookup>> Options(ErpDbContext db, CancellationToken ct) =>
        db.UnitsOfMeasure.AsNoTracking()
            .OrderBy(u => u.Code)
            .Select(u => new ItemLookup(u.Id, u.Code, u.Name, u.IsActive, null))
            .ToListAsync(ct);

    private static async Task<Results<Ok<UomRow>, NotFound>> Get(int id, ErpDbContext db, CancellationToken ct) =>
        await Rows(db.UnitsOfMeasure.AsNoTracking().Where(u => u.Id == id)).SingleOrDefaultAsync(ct) is { } row
            ? TypedResults.Ok(row)
            : TypedResults.NotFound();

    private static async Task<Results<Created<UomRow>, ValidationProblem, ProblemHttpResult>> Create(
        SaveUomRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, null, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var (code, name) = Normalize(request);
        if (await DuplicatesAsync(db, null, code, name, ct) is { } invalid) return invalid;

        var uom = new UnitOfMeasure
        {
            AccountId = current.Scope().AccountId,
            Code = code,
            Name = name,
        };
        Apply(uom, request);
        db.UnitsOfMeasure.Add(uom);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        var row = await Rows(db.UnitsOfMeasure.AsNoTracking().Where(u => u.Id == uom.Id)).SingleAsync(ct);
        return TypedResults.Created($"/api/erp/item-management/uoms/{uom.Id}", row);
    }

    private static async Task<Results<Ok<UomRow>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveUomRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var uom = await db.UnitsOfMeasure.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (uom is null) return TypedResults.NotFound();

        var existing = await Rows(db.UnitsOfMeasure.AsNoTracking().Where(u => u.Id == id)).SingleAsync(ct);
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, existing, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var (code, name) = Normalize(request);
        if (await DuplicatesAsync(db, id, code, name, ct) is { } invalid) return invalid;

        var renamed = uom.Name != name;
        uom.Code = code;
        uom.Name = name;
        Apply(uom, request);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            if (renamed)
            {
                var accountId = uom.AccountId;
                var items = db.Items.AcrossTenants().Where(i => i.AccountId == accountId);
                await items.Where(i => i.UomId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.UomName, name), ct);
                await items.Where(i => i.AltUomId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.AltUomName, name), ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        return TypedResults.Ok(await Rows(db.UnitsOfMeasure.AsNoTracking().Where(u => u.Id == id)).SingleAsync(ct));
    }

    private static Task<Results<Ok<AffectedRecords>, NotFound>> SetStatus(
        RecordStatusRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.SetStatusAsync<UnitOfMeasure>(db, request, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> Delete(
        int id, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<UnitOfMeasure>(db, [id], NotInUse, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> DeleteMany(
        RecordIdsRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<UnitOfMeasure>(db, request.Ids, NotInUse, ct);

    private static Task<string?> NotInUse(List<int> ids, CancellationToken ct) => Task.FromResult<string?>(null);

    private static (string Code, string Name) Normalize(SaveUomRequest request) => (request.Code.Trim(), request.Name.Trim());

    private static void Apply(UnitOfMeasure uom, SaveUomRequest request)
    {
        uom.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        uom.IsDecimalAllowed = request.IsDecimalAllowed;
        uom.RoundingLength = request.IsDecimalAllowed ? request.RoundingLength : 0;
        uom.IsActive = request.IsActive;
        uom.CustomData = CustomFieldData.ToJson(request.CustomData);
    }

    private static async Task<ValidationProblem?> DuplicatesAsync(
        ErpDbContext db, int? id, string code, string name, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (await db.UnitsOfMeasure.AnyAsync(u => u.Id != id && u.Code == code, ct))
            errors["code"] = ["Another unit already uses this code."];
        if (await db.UnitsOfMeasure.AnyAsync(u => u.Id != id && u.Name == name, ct))
            errors["name"] = ["Another unit already has this name."];
        return MasterData.Invalid(errors);
    }
}
