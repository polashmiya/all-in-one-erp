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

namespace Erp.Modules.Inventory.ItemManagement.Departments;

internal static class ItemDepartmentEndpoints
{
    private const string Feature = ItemManagementMenu.ItemDepartment;

    public static void Map(IEndpointRouteBuilder group)
    {
        var departments = group.MapGroup("/departments").WithTags("ItemDepartments");

        departments.MapGet("", List)
            .WithName("ListItemDepartments")
            .WithSummary("Departments of the current business unit with status counts")
            .RequirePermission($"{Feature}.view");

        departments.MapGet("/options", Options)
            .WithName("GetItemDepartmentOptions")
            .WithSummary("Every department of the business unit, for dropdowns")
            .RequireAnyPermission(ItemManagementMenu.LookupReaders);

        departments.MapGet("/{id:int}", Get)
            .WithName("GetItemDepartment")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        departments.MapPost("", Create)
            .WithName("CreateItemDepartment")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveItemDepartmentRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        departments.MapPut("/{id:int}", Update)
            .WithName("UpdateItemDepartment")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveItemDepartmentRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        departments.MapPost("/status", SetStatus)
            .WithName("SetItemDepartmentsStatus")
            .WithSummary("Activate or deactivate departments")
            .RequirePermission($"{Feature}.edit")
            .Validate<RecordStatusRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        departments.MapDelete("/{id:int}", Delete)
            .WithName("DeleteItemDepartment")
            .WithSummary("Delete a department that has no categories")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        departments.MapPost("/delete", DeleteMany)
            .WithName("DeleteItemDepartments")
            .WithSummary("Delete several departments; none is deleted if any has categories")
            .RequirePermission($"{Feature}.delete")
            .Validate<RecordIdsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static IQueryable<ItemDepartmentRow> Rows(IQueryable<ItemDepartment> query, ErpDbContext db) =>
        query.Select(d => new ItemDepartmentRow(
            d.Id,
            d.Code,
            d.Name,
            d.Serial,
            d.ImageSource,
            d.IsActive,
            db.ItemCategories.Count(c => c.DepartmentId == d.Id),
            d.CreatedAt,
            d.ModifiedAt,
            CustomFieldData.Parse(d.CustomData)));

    private static async Task<ItemDepartmentList> List(
        string? search, string? status, string? sort, int? page, int? pageSize, ErpDbContext db, CancellationToken ct)
    {
        var query = db.ItemDepartments.AsNoTracking();
        if (MasterData.Search(search) is { } term) query = query.Where(d => d.Name.Contains(term) || d.Code.Contains(term));

        var counts = await query.CountByStatusAsync(ct);
        query = query.WithStatus(status);

        var sorted = sort switch
        {
            "code" => query.OrderBy(d => d.Code),
            "-code" => query.OrderByDescending(d => d.Code),
            "name" => query.OrderBy(d => d.Name),
            "-name" => query.OrderByDescending(d => d.Name),
            "-serial" => query.OrderByDescending(d => d.Serial).ThenBy(d => d.Name),
            "modifiedAt" => query.OrderBy(d => d.ModifiedAt ?? d.CreatedAt),
            "-modifiedAt" => query.OrderByDescending(d => d.ModifiedAt ?? d.CreatedAt),
            _ => query.OrderBy(d => d.Serial == null).ThenBy(d => d.Serial).ThenBy(d => d.Name),
        };

        var items = await Rows(sorted.ThenBy(d => d.Id).Page(page, pageSize), db).ToListAsync(ct);
        return new ItemDepartmentList(items, counts.TotalFor(status), counts);
    }

    private static Task<List<ItemLookup>> Options(ErpDbContext db, CancellationToken ct) =>
        db.ItemDepartments.AsNoTracking()
            .OrderBy(d => d.Serial == null).ThenBy(d => d.Serial).ThenBy(d => d.Name)
            .Select(d => new ItemLookup(d.Id, d.Code, d.Name, d.IsActive, null))
            .ToListAsync(ct);

    private static async Task<Results<Ok<ItemDepartmentRow>, NotFound>> Get(int id, ErpDbContext db, CancellationToken ct) =>
        await Rows(db.ItemDepartments.AsNoTracking().Where(d => d.Id == id), db).SingleOrDefaultAsync(ct) is { } row
            ? TypedResults.Ok(row)
            : TypedResults.NotFound();

    private static async Task<Results<Created<ItemDepartmentRow>, ValidationProblem, ProblemHttpResult>> Create(
        SaveItemDepartmentRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, null, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var (code, name) = Normalize(request);
        if (await DuplicatesAsync(db, null, code, name, ct) is { } invalid) return invalid;

        var (accountId, businessUnitId) = current.Scope();
        var department = new ItemDepartment
        {
            AccountId = accountId,
            BusinessUnitId = businessUnitId,
            Code = code,
            Name = name,
            Serial = request.Serial ?? await NextSerialAsync(db, ct),
            ImageSource = MasterData.CleanImage(request.ImageSource),
            IsActive = request.IsActive,
            CustomData = CustomFieldData.ToJson(request.CustomData),
        };
        db.ItemDepartments.Add(department);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        var row = await Rows(db.ItemDepartments.AsNoTracking().Where(d => d.Id == department.Id), db).SingleAsync(ct);
        return TypedResults.Created($"/api/erp/item-management/departments/{department.Id}", row);
    }

    private static async Task<Results<Ok<ItemDepartmentRow>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveItemDepartmentRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var department = await db.ItemDepartments.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (department is null) return TypedResults.NotFound();

        var existing = await Rows(db.ItemDepartments.AsNoTracking().Where(d => d.Id == id), db).SingleAsync(ct);
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, existing, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var (code, name) = Normalize(request);
        if (await DuplicatesAsync(db, id, code, name, ct) is { } invalid) return invalid;

        department.Code = code;
        department.Name = name;
        department.Serial = request.Serial;
        department.ImageSource = MasterData.CleanImage(request.ImageSource);
        department.IsActive = request.IsActive;
        department.CustomData = CustomFieldData.ToJson(request.CustomData);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        return TypedResults.Ok(await Rows(db.ItemDepartments.AsNoTracking().Where(d => d.Id == id), db).SingleAsync(ct));
    }

    private static Task<Results<Ok<AffectedRecords>, NotFound>> SetStatus(
        RecordStatusRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.SetStatusAsync<ItemDepartment>(db, request, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> Delete(
        int id, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemDepartment>(db, [id], (ids, token) => UsageAsync(db, ids, token), ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> DeleteMany(
        RecordIdsRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemDepartment>(db, request.Ids, (ids, token) => UsageAsync(db, ids, token), ct);

    private static (string Code, string Name) Normalize(SaveItemDepartmentRequest request) =>
        (request.Code.Trim().ToUpperInvariant(), request.Name.Trim());

    private static async Task<ValidationProblem?> DuplicatesAsync(
        ErpDbContext db, int? id, string code, string name, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (await db.ItemDepartments.AnyAsync(d => d.Id != id && d.Code == code, ct))
            errors["code"] = ["Another department already uses this code."];
        if (await db.ItemDepartments.AnyAsync(d => d.Id != id && d.Name == name, ct))
            errors["name"] = ["Another department already has this name."];
        return MasterData.Invalid(errors);
    }

    private static async Task<int> NextSerialAsync(ErpDbContext db, CancellationToken ct) =>
        (await db.ItemDepartments.MaxAsync(d => d.Serial, ct) ?? 0) + 1;

    private static async Task<string?> UsageAsync(ErpDbContext db, List<int> ids, CancellationToken ct)
    {
        var used = await db.ItemDepartments
            .Where(d => ids.Contains(d.Id))
            .Select(d => new { d.Name, Categories = db.ItemCategories.Count(c => c.DepartmentId == d.Id) })
            .Where(x => x.Categories > 0)
            .FirstOrDefaultAsync(ct);

        return used is null
            ? null
            : $"“{used.Name}” has {MasterData.Plural(used.Categories, "category", "categories")}. Delete or move them first, or deactivate the department instead.";
    }
}
