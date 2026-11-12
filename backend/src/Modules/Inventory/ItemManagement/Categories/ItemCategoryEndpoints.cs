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

namespace Erp.Modules.Inventory.ItemManagement.Categories;

internal static class ItemCategoryEndpoints
{
    private const string Feature = ItemManagementMenu.ItemCategory;

    public static void Map(IEndpointRouteBuilder group)
    {
        var categories = group.MapGroup("/categories").WithTags("ItemCategories");

        categories.MapGet("", List)
            .WithName("ListItemCategories")
            .WithSummary("Categories of the current business unit with status counts")
            .RequirePermission($"{Feature}.view");

        categories.MapGet("/options", Options)
            .WithName("GetItemCategoryOptions")
            .WithSummary("Categories for dropdowns, optionally of one department")
            .RequireAnyPermission(ItemManagementMenu.LookupReaders);

        categories.MapGet("/{id:int}", Get)
            .WithName("GetItemCategory")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        categories.MapPost("", Create)
            .WithName("CreateItemCategory")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveItemCategoryRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        categories.MapPut("/{id:int}", Update)
            .WithName("UpdateItemCategory")
            .WithSummary("Update a category; moving it to another department moves its sub categories too")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveItemCategoryRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        categories.MapPost("/status", SetStatus)
            .WithName("SetItemCategoriesStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<RecordStatusRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        categories.MapDelete("/{id:int}", Delete)
            .WithName("DeleteItemCategory")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        categories.MapPost("/delete", DeleteMany)
            .WithName("DeleteItemCategories")
            .RequirePermission($"{Feature}.delete")
            .Validate<RecordIdsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static IQueryable<ItemCategoryRow> Rows(IQueryable<ItemCategory> query, ErpDbContext db) =>
        query.Select(c => new ItemCategoryRow(
            c.Id,
            c.Code,
            c.Name,
            c.DepartmentId,
            c.Department!.Name,
            c.ImageSource,
            c.IsActive,
            db.ItemSubCategories.Count(s => s.CategoryId == c.Id),
            c.CreatedAt,
            c.ModifiedAt,
            CustomFieldData.Parse(c.CustomData)));

    private static async Task<ItemCategoryList> List(
        string? search,
        string? status,
        string? sort,
        int? page,
        int? pageSize,
        int? departmentId,
        ErpDbContext db,
        CancellationToken ct)
    {
        var query = db.ItemCategories.AsNoTracking();
        if (departmentId is not null) query = query.Where(c => c.DepartmentId == departmentId);
        if (MasterData.Search(search) is { } term) query = query.Where(c => c.Name.Contains(term) || c.Code.Contains(term));

        var counts = await query.CountByStatusAsync(ct);
        query = query.WithStatus(status);

        var sorted = MasterData.SortKey(sort) switch
        {
            "code" => query.OrderBy(c => c.Code),
            "-code" => query.OrderByDescending(c => c.Code),
            "-name" => query.OrderByDescending(c => c.Name),
            "department" => query.OrderBy(c => c.Department!.Name).ThenBy(c => c.Name),
            "-department" => query.OrderByDescending(c => c.Department!.Name).ThenBy(c => c.Name),
            "modifiedAt" => query.OrderBy(c => c.ModifiedAt ?? c.CreatedAt),
            "-modifiedAt" => query.OrderByDescending(c => c.ModifiedAt ?? c.CreatedAt),
            _ => query.OrderBy(c => c.Name),
        };

        var items = await Rows(sorted.ThenBy(c => c.Id).Page(page, pageSize), db).ToListAsync(ct);
        return new ItemCategoryList(items, counts.TotalFor(status), counts);
    }

    private static Task<List<ItemLookup>> Options(int? departmentId, ErpDbContext db, CancellationToken ct) =>
        db.ItemCategories.AsNoTracking()
            .Where(c => departmentId == null || c.DepartmentId == departmentId)
            .OrderBy(c => c.Name)
            .Select(c => new ItemLookup(c.Id, c.Code, c.Name, c.IsActive, c.DepartmentId))
            .ToListAsync(ct);

    private static async Task<Results<Ok<ItemCategoryRow>, NotFound>> Get(int id, ErpDbContext db, CancellationToken ct) =>
        await Rows(db.ItemCategories.AsNoTracking().Where(c => c.Id == id), db).SingleOrDefaultAsync(ct) is { } row
            ? TypedResults.Ok(row)
            : TypedResults.NotFound();

    private static async Task<Results<Created<ItemCategoryRow>, ValidationProblem, ProblemHttpResult>> Create(
        SaveItemCategoryRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, null, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var (code, name) = Normalize(request);
        if (await CheckDepartmentAsync(db, request.DepartmentId, null, ct) is { } badDepartment) return badDepartment;
        if (await DuplicatesAsync(db, null, request.DepartmentId, code, name, ct) is { } invalid) return invalid;

        var (accountId, businessUnitId) = current.Scope();
        var category = new ItemCategory
        {
            AccountId = accountId,
            BusinessUnitId = businessUnitId,
            DepartmentId = request.DepartmentId,
            Code = code,
            Name = name,
            ImageSource = MasterData.CleanImage(request.ImageSource),
            IsActive = request.IsActive,
            CustomData = CustomFieldData.ToJson(request.CustomData),
        };
        db.ItemCategories.Add(category);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        var row = await Rows(db.ItemCategories.AsNoTracking().Where(c => c.Id == category.Id), db).SingleAsync(ct);
        return TypedResults.Created($"/api/erp/item-management/categories/{category.Id}", row);
    }

    private static async Task<Results<Ok<ItemCategoryRow>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveItemCategoryRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var category = await db.ItemCategories.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return TypedResults.NotFound();

        var existing = await Rows(db.ItemCategories.AsNoTracking().Where(c => c.Id == id), db).SingleAsync(ct);
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, existing, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var (code, name) = Normalize(request);
        if (await CheckDepartmentAsync(db, request.DepartmentId, category.DepartmentId, ct) is { } badDepartment) return badDepartment;
        if (await DuplicatesAsync(db, id, request.DepartmentId, code, name, ct) is { } invalid) return invalid;

        var moved = category.DepartmentId != request.DepartmentId;
        category.DepartmentId = request.DepartmentId;
        category.Code = code;
        category.Name = name;
        category.ImageSource = MasterData.CleanImage(request.ImageSource);
        category.IsActive = request.IsActive;
        category.CustomData = CustomFieldData.ToJson(request.CustomData);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            if (moved)
            {
                int? departmentId = request.DepartmentId;
                await db.ItemSubCategories.Where(s => s.CategoryId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.DepartmentId, departmentId), ct);
                await db.Items.Where(i => i.CategoryId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.DepartmentId, request.DepartmentId), ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        return TypedResults.Ok(await Rows(db.ItemCategories.AsNoTracking().Where(c => c.Id == id), db).SingleAsync(ct));
    }

    private static Task<Results<Ok<AffectedRecords>, NotFound>> SetStatus(
        RecordStatusRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.SetStatusAsync<ItemCategory>(db, request, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> Delete(
        int id, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemCategory>(db, [id], (ids, token) => UsageAsync(db, ids, token), ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> DeleteMany(
        RecordIdsRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemCategory>(db, request.Ids, (ids, token) => UsageAsync(db, ids, token), ct);

    private static (string Code, string Name) Normalize(SaveItemCategoryRequest request) =>
        (request.Code.Trim().ToUpperInvariant(), request.Name.Trim());

    private static async Task<ValidationProblem?> CheckDepartmentAsync(
        ErpDbContext db, int departmentId, int? currentDepartmentId, CancellationToken ct)
    {
        var department = await db.ItemDepartments.Where(d => d.Id == departmentId)
            .Select(d => new { d.IsActive })
            .SingleOrDefaultAsync(ct);

        if (department is null) return MasterData.Invalid("departmentId", "Choose a department of this business unit.");
        if (!department.IsActive && departmentId != currentDepartmentId)
            return MasterData.Invalid("departmentId", "This department is inactive. Activate it or choose another.");
        return null;
    }

    private static async Task<ValidationProblem?> DuplicatesAsync(
        ErpDbContext db, int? id, int departmentId, string code, string name, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (await db.ItemCategories.AnyAsync(c => c.Id != id && c.Code == code, ct))
            errors["code"] = ["Another category already uses this code."];
        if (await db.ItemCategories.AnyAsync(c => c.Id != id && c.DepartmentId == departmentId && c.Name == name, ct))
            errors["name"] = ["This department already has a category with this name."];
        return MasterData.Invalid(errors);
    }

    private static async Task<string?> UsageAsync(ErpDbContext db, List<int> ids, CancellationToken ct)
    {
        var used = await db.ItemCategories
            .Where(c => ids.Contains(c.Id))
            .Select(c => new
            {
                c.Name,
                SubCategories = db.ItemSubCategories.Count(s => s.CategoryId == c.Id),
                MinorCategories = db.ItemMinorCategories.Count(m => m.CategoryId == c.Id),
            })
            .Where(x => x.SubCategories > 0 || x.MinorCategories > 0)
            .FirstOrDefaultAsync(ct);

        return used is null
            ? null
            : $"“{used.Name}” has {MasterData.Plural(used.SubCategories, "sub category", "sub categories")}. Delete or move them first, or deactivate the category instead.";
    }
}
