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

namespace Erp.Modules.Inventory.ItemManagement.SubCategories;

internal static class ItemSubCategoryEndpoints
{
    private const string Feature = ItemManagementMenu.ItemSubCategory;

    public static void Map(IEndpointRouteBuilder group)
    {
        var subCategories = group.MapGroup("/sub-categories").WithTags("ItemSubCategories");

        subCategories.MapGet("", List)
            .WithName("ListItemSubCategories")
            .WithSummary("Sub categories of the current business unit with status counts")
            .RequirePermission($"{Feature}.view");

        subCategories.MapGet("/options", Options)
            .WithName("GetItemSubCategoryOptions")
            .WithSummary("Sub categories for dropdowns, optionally of one category")
            .RequireAnyPermission(ItemManagementMenu.LookupReaders);

        subCategories.MapGet("/{id:int}", Get)
            .WithName("GetItemSubCategory")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        subCategories.MapPost("", Create)
            .WithName("CreateItemSubCategory")
            .WithSummary("Create a sub category; its department is taken from the category")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveItemSubCategoryRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        subCategories.MapPut("/{id:int}", Update)
            .WithName("UpdateItemSubCategory")
            .WithSummary("Update a sub category; moving it to another category moves its minor categories too")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveItemSubCategoryRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        subCategories.MapPost("/status", SetStatus)
            .WithName("SetItemSubCategoriesStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<RecordStatusRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        subCategories.MapDelete("/{id:int}", Delete)
            .WithName("DeleteItemSubCategory")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        subCategories.MapPost("/delete", DeleteMany)
            .WithName("DeleteItemSubCategories")
            .RequirePermission($"{Feature}.delete")
            .Validate<RecordIdsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static IQueryable<ItemSubCategoryRow> Rows(IQueryable<ItemSubCategory> query, ErpDbContext db) =>
        query.Select(s => new ItemSubCategoryRow(
            s.Id,
            s.Name,
            s.Category!.DepartmentId,
            s.Category.Department!.Name,
            s.CategoryId,
            s.Category.Name,
            s.ImageSource,
            s.IsActive,
            db.ItemMinorCategories.Count(m => m.SubCategoryId == s.Id),
            s.CreatedAt,
            s.ModifiedAt,
            CustomFieldData.Parse(s.CustomData)));

    private static async Task<ItemSubCategoryList> List(
        string? search,
        string? status,
        string? sort,
        int? page,
        int? pageSize,
        int? departmentId,
        int? categoryId,
        ErpDbContext db,
        CancellationToken ct)
    {
        var query = db.ItemSubCategories.AsNoTracking();
        if (departmentId is not null) query = query.Where(s => s.Category!.DepartmentId == departmentId);
        if (categoryId is not null) query = query.Where(s => s.CategoryId == categoryId);
        if (MasterData.Search(search) is { } term) query = query.Where(s => s.Name.Contains(term));

        var counts = await query.CountByStatusAsync(ct);
        query = query.WithStatus(status);

        var sorted = MasterData.SortKey(sort) switch
        {
            "name" => query.OrderBy(s => s.Name),
            "-name" => query.OrderByDescending(s => s.Name),
            "-category" => query.OrderByDescending(s => s.Category!.Name).ThenBy(s => s.Name),
            "department" => query.OrderBy(s => s.Category!.Department!.Name).ThenBy(s => s.Category!.Name).ThenBy(s => s.Name),
            "-department" => query.OrderByDescending(s => s.Category!.Department!.Name).ThenBy(s => s.Category!.Name).ThenBy(s => s.Name),
            "modifiedAt" => query.OrderBy(s => s.ModifiedAt ?? s.CreatedAt),
            "-modifiedAt" => query.OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt),
            _ => query.OrderBy(s => s.Category!.Name).ThenBy(s => s.Name),
        };

        var items = await Rows(sorted.ThenBy(s => s.Id).Page(page, pageSize), db).ToListAsync(ct);
        return new ItemSubCategoryList(items, counts.TotalFor(status), counts);
    }

    private static Task<List<ItemLookup>> Options(int? categoryId, ErpDbContext db, CancellationToken ct) =>
        db.ItemSubCategories.AsNoTracking()
            .Where(s => categoryId == null || s.CategoryId == categoryId)
            .OrderBy(s => s.Name)
            .Select(s => new ItemLookup(s.Id, null, s.Name, s.IsActive, s.CategoryId))
            .ToListAsync(ct);

    private static async Task<Results<Ok<ItemSubCategoryRow>, NotFound>> Get(int id, ErpDbContext db, CancellationToken ct) =>
        await Rows(db.ItemSubCategories.AsNoTracking().Where(s => s.Id == id), db).SingleOrDefaultAsync(ct) is { } row
            ? TypedResults.Ok(row)
            : TypedResults.NotFound();

    private static async Task<Results<Created<ItemSubCategoryRow>, ValidationProblem, ProblemHttpResult>> Create(
        SaveItemSubCategoryRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, null, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var name = request.Name.Trim();
        var category = await FindCategoryAsync(db, request.CategoryId, null, ct);
        if (category.Problem is { } badCategory) return badCategory;
        if (await DuplicateAsync(db, null, request.CategoryId, name, ct) is { } invalid) return invalid;

        var (accountId, businessUnitId) = current.Scope();
        var subCategory = new ItemSubCategory
        {
            AccountId = accountId,
            BusinessUnitId = businessUnitId,
            CategoryId = request.CategoryId,
            DepartmentId = category.DepartmentId,
            Name = name,
            ImageSource = MasterData.CleanImage(request.ImageSource),
            IsActive = request.IsActive,
            CustomData = CustomFieldData.ToJson(request.CustomData),
        };
        db.ItemSubCategories.Add(subCategory);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        var row = await Rows(db.ItemSubCategories.AsNoTracking().Where(s => s.Id == subCategory.Id), db).SingleAsync(ct);
        return TypedResults.Created($"/api/erp/item-management/sub-categories/{subCategory.Id}", row);
    }

    private static async Task<Results<Ok<ItemSubCategoryRow>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveItemSubCategoryRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var subCategory = await db.ItemSubCategories.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (subCategory is null) return TypedResults.NotFound();

        var existing = await Rows(db.ItemSubCategories.AsNoTracking().Where(s => s.Id == id), db).SingleAsync(ct);
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, existing, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var name = request.Name.Trim();
        var category = await FindCategoryAsync(db, request.CategoryId, subCategory.CategoryId, ct);
        if (category.Problem is { } badCategory) return badCategory;
        if (await DuplicateAsync(db, id, request.CategoryId, name, ct) is { } invalid) return invalid;

        var moved = subCategory.CategoryId != request.CategoryId;
        subCategory.CategoryId = request.CategoryId;
        subCategory.DepartmentId = category.DepartmentId;
        subCategory.Name = name;
        subCategory.ImageSource = MasterData.CleanImage(request.ImageSource);
        subCategory.IsActive = request.IsActive;
        subCategory.CustomData = CustomFieldData.ToJson(request.CustomData);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            if (moved)
            {
                var (categoryId, departmentId) = (request.CategoryId, category.DepartmentId);
                await db.ItemMinorCategories.Where(m => m.SubCategoryId == id)
                    .ExecuteUpdateAsync(m => m.SetProperty(x => x.CategoryId, categoryId), ct);
                await db.Items.Where(i => i.SubCategoryId == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.CategoryId, categoryId)
                        .SetProperty(x => x.DepartmentId, departmentId), ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        return TypedResults.Ok(await Rows(db.ItemSubCategories.AsNoTracking().Where(s => s.Id == id), db).SingleAsync(ct));
    }

    private static Task<Results<Ok<AffectedRecords>, NotFound>> SetStatus(
        RecordStatusRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.SetStatusAsync<ItemSubCategory>(db, request, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> Delete(
        int id, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemSubCategory>(db, [id], (ids, token) => UsageAsync(db, ids, token), ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> DeleteMany(
        RecordIdsRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemSubCategory>(db, request.Ids, (ids, token) => UsageAsync(db, ids, token), ct);

    private sealed record CategoryCheck(int DepartmentId, ValidationProblem? Problem);

    private static async Task<CategoryCheck> FindCategoryAsync(
        ErpDbContext db, int categoryId, int? currentCategoryId, CancellationToken ct)
    {
        var category = await db.ItemCategories.Where(c => c.Id == categoryId)
            .Select(c => new { c.DepartmentId, c.IsActive })
            .SingleOrDefaultAsync(ct);

        if (category is null) return new(0, MasterData.Invalid("categoryId", "Choose a category of this business unit."));
        if (!category.IsActive && categoryId != currentCategoryId)
            return new(0, MasterData.Invalid("categoryId", "This category is inactive. Activate it or choose another."));
        return new(category.DepartmentId, null);
    }

    private static async Task<ValidationProblem?> DuplicateAsync(
        ErpDbContext db, int? id, int categoryId, string name, CancellationToken ct) =>
        await db.ItemSubCategories.AnyAsync(s => s.Id != id && s.CategoryId == categoryId && s.Name == name, ct)
            ? MasterData.Invalid("name", "This category already has a sub category with this name.")
            : null;

    private static async Task<string?> UsageAsync(ErpDbContext db, List<int> ids, CancellationToken ct)
    {
        var used = await db.ItemSubCategories
            .Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Name, MinorCategories = db.ItemMinorCategories.Count(m => m.SubCategoryId == s.Id) })
            .Where(x => x.MinorCategories > 0)
            .FirstOrDefaultAsync(ct);

        return used is null
            ? null
            : $"“{used.Name}” has {MasterData.Plural(used.MinorCategories, "minor category", "minor categories")}. Delete or move them first, or deactivate the sub category instead.";
    }
}
