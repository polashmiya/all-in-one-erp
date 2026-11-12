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

namespace Erp.Modules.Inventory.ItemManagement.MinorCategories;

internal static class ItemMinorCategoryEndpoints
{
    private const string Feature = ItemManagementMenu.ItemMinorCategory;

    public static void Map(IEndpointRouteBuilder group)
    {
        var minorCategories = group.MapGroup("/minor-categories").WithTags("ItemMinorCategories");

        minorCategories.MapGet("", List)
            .WithName("ListItemMinorCategories")
            .WithSummary("Minor categories of the current business unit with status counts")
            .RequirePermission($"{Feature}.view");

        minorCategories.MapGet("/options", Options)
            .WithName("GetItemMinorCategoryOptions")
            .WithSummary("Minor categories for dropdowns, optionally of one sub category")
            .RequireAnyPermission(ItemManagementMenu.LookupReaders);

        minorCategories.MapGet("/{id:int}", Get)
            .WithName("GetItemMinorCategory")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        minorCategories.MapPost("", Create)
            .WithName("CreateItemMinorCategory")
            .WithSummary("Create a minor category; its category is taken from the sub category")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveItemMinorCategoryRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        minorCategories.MapPut("/{id:int}", Update)
            .WithName("UpdateItemMinorCategory")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveItemMinorCategoryRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        minorCategories.MapPost("/status", SetStatus)
            .WithName("SetItemMinorCategoriesStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<RecordStatusRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        minorCategories.MapDelete("/{id:int}", Delete)
            .WithName("DeleteItemMinorCategory")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        minorCategories.MapPost("/delete", DeleteMany)
            .WithName("DeleteItemMinorCategories")
            .RequirePermission($"{Feature}.delete")
            .Validate<RecordIdsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static IQueryable<ItemMinorCategoryRow> Rows(IQueryable<ItemMinorCategory> query) =>
        query.Select(m => new ItemMinorCategoryRow(
            m.Id,
            m.Name,
            m.Category!.DepartmentId,
            m.Category.Department!.Name,
            m.CategoryId,
            m.Category.Name,
            m.SubCategoryId,
            m.SubCategory!.Name,
            m.ImageSource,
            m.IsActive,
            m.CreatedAt,
            m.ModifiedAt,
            CustomFieldData.Parse(m.CustomData)));

    private static async Task<ItemMinorCategoryList> List(
        string? search,
        string? status,
        string? sort,
        int? page,
        int? pageSize,
        int? departmentId,
        int? categoryId,
        int? subCategoryId,
        ErpDbContext db,
        CancellationToken ct)
    {
        var query = db.ItemMinorCategories.AsNoTracking();
        if (departmentId is not null) query = query.Where(m => m.Category!.DepartmentId == departmentId);
        if (categoryId is not null) query = query.Where(m => m.CategoryId == categoryId);
        if (subCategoryId is not null) query = query.Where(m => m.SubCategoryId == subCategoryId);
        if (MasterData.Search(search) is { } term) query = query.Where(m => m.Name.Contains(term));

        var counts = await query.CountByStatusAsync(ct);
        query = query.WithStatus(status);

        var sorted = MasterData.SortKey(sort) switch
        {
            "name" => query.OrderBy(m => m.Name),
            "-name" => query.OrderByDescending(m => m.Name),
            "category" => query.OrderBy(m => m.Category!.Name).ThenBy(m => m.SubCategory!.Name).ThenBy(m => m.Name),
            "-category" => query.OrderByDescending(m => m.Category!.Name).ThenBy(m => m.SubCategory!.Name).ThenBy(m => m.Name),
            "-subCategory" => query.OrderByDescending(m => m.SubCategory!.Name).ThenBy(m => m.Name),
            "modifiedAt" => query.OrderBy(m => m.ModifiedAt ?? m.CreatedAt),
            "-modifiedAt" => query.OrderByDescending(m => m.ModifiedAt ?? m.CreatedAt),
            _ => query.OrderBy(m => m.SubCategory!.Name).ThenBy(m => m.Name),
        };

        var items = await Rows(sorted.ThenBy(m => m.Id).Page(page, pageSize)).ToListAsync(ct);
        return new ItemMinorCategoryList(items, counts.TotalFor(status), counts);
    }

    private static Task<List<ItemLookup>> Options(int? subCategoryId, ErpDbContext db, CancellationToken ct) =>
        db.ItemMinorCategories.AsNoTracking()
            .Where(m => subCategoryId == null || m.SubCategoryId == subCategoryId)
            .OrderBy(m => m.Name)
            .Select(m => new ItemLookup(m.Id, null, m.Name, m.IsActive, m.SubCategoryId))
            .ToListAsync(ct);

    private static async Task<Results<Ok<ItemMinorCategoryRow>, NotFound>> Get(int id, ErpDbContext db, CancellationToken ct) =>
        await Rows(db.ItemMinorCategories.AsNoTracking().Where(m => m.Id == id)).SingleOrDefaultAsync(ct) is { } row
            ? TypedResults.Ok(row)
            : TypedResults.NotFound();

    private static async Task<Results<Created<ItemMinorCategoryRow>, ValidationProblem, ProblemHttpResult>> Create(
        SaveItemMinorCategoryRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, null, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var name = request.Name.Trim();
        var subCategory = await FindSubCategoryAsync(db, request.SubCategoryId, null, ct);
        if (subCategory.Problem is { } badSubCategory) return badSubCategory;
        if (await DuplicateAsync(db, null, request.SubCategoryId, name, ct) is { } invalid) return invalid;

        var (accountId, businessUnitId) = current.Scope();
        var minorCategory = new ItemMinorCategory
        {
            AccountId = accountId,
            BusinessUnitId = businessUnitId,
            CategoryId = subCategory.CategoryId,
            SubCategoryId = request.SubCategoryId,
            Name = name,
            ImageSource = MasterData.CleanImage(request.ImageSource),
            IsActive = request.IsActive,
            CustomData = CustomFieldData.ToJson(request.CustomData),
        };
        db.ItemMinorCategories.Add(minorCategory);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        var row = await Rows(db.ItemMinorCategories.AsNoTracking().Where(m => m.Id == minorCategory.Id)).SingleAsync(ct);
        return TypedResults.Created($"/api/erp/item-management/minor-categories/{minorCategory.Id}", row);
    }

    private static async Task<Results<Ok<ItemMinorCategoryRow>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveItemMinorCategoryRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var minorCategory = await db.ItemMinorCategories.SingleOrDefaultAsync(m => m.Id == id, ct);
        if (minorCategory is null) return TypedResults.NotFound();

        var existing = await Rows(db.ItemMinorCategories.AsNoTracking().Where(m => m.Id == id)).SingleAsync(ct);
        var (configured, rejected) = await MasterData.ConfiguredAsync(configs, current, Feature, request, existing, ct);
        if (rejected is not null) return rejected;
        request = configured;

        var name = request.Name.Trim();
        var subCategory = await FindSubCategoryAsync(db, request.SubCategoryId, minorCategory.SubCategoryId, ct);
        if (subCategory.Problem is { } badSubCategory) return badSubCategory;
        if (await DuplicateAsync(db, id, request.SubCategoryId, name, ct) is { } invalid) return invalid;

        var moved = minorCategory.SubCategoryId != request.SubCategoryId;
        minorCategory.SubCategoryId = request.SubCategoryId;
        minorCategory.CategoryId = subCategory.CategoryId;
        minorCategory.Name = name;
        minorCategory.ImageSource = MasterData.CleanImage(request.ImageSource);
        minorCategory.IsActive = request.IsActive;
        minorCategory.CustomData = CustomFieldData.ToJson(request.CustomData);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            if (moved)
            {
                var (subCategoryId, categoryId, departmentId) = (request.SubCategoryId, subCategory.CategoryId, subCategory.DepartmentId);
                await db.Items.Where(i => i.MinorCategoryId == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.SubCategoryId, subCategoryId)
                        .SetProperty(x => x.CategoryId, categoryId)
                        .SetProperty(x => x.DepartmentId, departmentId), ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }

        return TypedResults.Ok(await Rows(db.ItemMinorCategories.AsNoTracking().Where(m => m.Id == id)).SingleAsync(ct));
    }

    private static Task<Results<Ok<AffectedRecords>, NotFound>> SetStatus(
        RecordStatusRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.SetStatusAsync<ItemMinorCategory>(db, request, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> Delete(
        int id, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemMinorCategory>(db, [id], NotInUse, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> DeleteMany(
        RecordIdsRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<ItemMinorCategory>(db, request.Ids, NotInUse, ct);

    private static Task<string?> NotInUse(List<int> ids, CancellationToken ct) => Task.FromResult<string?>(null);

    private sealed record SubCategoryCheck(int CategoryId, int DepartmentId, ValidationProblem? Problem);

    private static async Task<SubCategoryCheck> FindSubCategoryAsync(
        ErpDbContext db, int subCategoryId, int? currentSubCategoryId, CancellationToken ct)
    {
        var subCategory = await db.ItemSubCategories.Where(s => s.Id == subCategoryId)
            .Select(s => new { s.CategoryId, s.Category!.DepartmentId, s.IsActive })
            .SingleOrDefaultAsync(ct);

        if (subCategory is null) return new(0, 0, MasterData.Invalid("subCategoryId", "Choose a sub category of this business unit."));
        if (!subCategory.IsActive && subCategoryId != currentSubCategoryId)
            return new(0, 0, MasterData.Invalid("subCategoryId", "This sub category is inactive. Activate it or choose another."));
        return new(subCategory.CategoryId, subCategory.DepartmentId, null);
    }

    private static async Task<ValidationProblem?> DuplicateAsync(
        ErpDbContext db, int? id, int subCategoryId, string name, CancellationToken ct) =>
        await db.ItemMinorCategories.AnyAsync(m => m.Id != id && m.SubCategoryId == subCategoryId && m.Name == name, ct)
            ? MasterData.Invalid("name", "This sub category already has a minor category with this name.")
            : null;
}
