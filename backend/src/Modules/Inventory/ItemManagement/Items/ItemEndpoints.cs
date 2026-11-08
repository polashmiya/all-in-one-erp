using Erp.Core.Authorization;
using Erp.Core.Domain.Inventory;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Inventory.ItemManagement.Items;

internal static class ItemEndpoints
{
    private const string Feature = ItemManagementMenu.ItemProfile;
    private const int OptionLimit = 20;

    public static void Map(IEndpointRouteBuilder group)
    {
        var items = group.MapGroup("/items").WithTags("Items");

        items.MapGet("", List)
            .WithName("ListItems")
            .WithSummary("Items of the current business unit with status counts")
            .RequirePermission($"{Feature}.view");

        items.MapGet("/choices", Choices)
            .WithName("GetItemChoices")
            .WithSummary("Item types and VAT supply types for dropdowns")
            .RequirePermission($"{Feature}.view");

        items.MapGet("/options", Options)
            .WithName("GetItemOptions")
            .WithSummary("Up to 20 items that can be a master item, matching the search")
            .RequirePermission($"{Feature}.view");

        items.MapGet("/{id:int}", Get)
            .WithName("GetItem")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapPost("", Create)
            .WithName("CreateItem")
            .WithSummary("Create an item; its department, category and sub category come from the minor category")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveItemRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        items.MapPut("/{id:int}", Update)
            .WithName("UpdateItem")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveItemRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        items.MapPost("/status", SetStatus)
            .WithName("SetItemsStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<RecordStatusRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapDelete("/{id:int}", Delete)
            .WithName("DeleteItem")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        items.MapPost("/delete", DeleteMany)
            .WithName("DeleteItems")
            .RequirePermission($"{Feature}.delete")
            .Validate<RecordIdsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static IQueryable<ItemRow> Rows(IQueryable<Item> query) =>
        query.Select(i => new ItemRow(
            i.Id,
            i.Code,
            i.Name,
            i.BarCode,
            i.ItemTypeName,
            i.Department!.Name,
            i.Category!.Name,
            i.SubCategory!.Name,
            i.MinorCategory!.Name,
            i.UomName,
            i.SalesPrice,
            i.ImageSource,
            i.IsActive,
            i.CreatedAt,
            i.ModifiedAt));

    private static IQueryable<ItemDetail> Details(ErpDbContext db, int id) =>
        db.Items.AsNoTracking().Where(i => i.Id == id).Select(i => new ItemDetail
        {
            Id = i.Id,
            Code = i.Code,
            Name = i.Name,
            Description = i.Description,
            ItemTypeId = i.ItemTypeId,
            ItemTypeName = i.ItemTypeName,
            HsCode = i.HsCode,
            DepartmentId = i.DepartmentId,
            DepartmentName = i.Department!.Name,
            CategoryId = i.CategoryId,
            CategoryName = i.Category!.Name,
            SubCategoryId = i.SubCategoryId,
            SubCategoryName = i.SubCategory!.Name,
            MinorCategoryId = i.MinorCategoryId,
            MinorCategoryName = i.MinorCategory!.Name,
            BarCode = i.BarCode,
            IsAutoBarcode = i.IsAutoBarcode,
            IsChildBarcode = i.IsChildBarcode,
            MasterItemId = i.MasterItemId,
            MasterItemCode = db.Items.Where(m => m.Id == i.MasterItemId).Select(m => m.Code).FirstOrDefault(),
            MasterItemName = db.Items.Where(m => m.Id == i.MasterItemId).Select(m => m.Name).FirstOrDefault(),
            SupplierName = i.SupplierName,
            UomId = i.UomId,
            UomName = i.UomName,
            AltUomId = i.AltUomId,
            AltUomName = i.AltUomName,
            AltUomConversionQty = i.AltUomConversionQty,
            IsNoDecimalQty = i.IsNoDecimalQty,
            AverageCostPrice = i.AverageCostPrice,
            LastCostPrice = i.LastCostPrice,
            SalesPrice = i.SalesPrice,
            PurchaseSupplyTypeId = i.PurchaseSupplyTypeId,
            PurchaseSupplyTypeName = i.PurchaseSupplyTypeName,
            PurchaseVatPercentage = i.PurchaseVatPercentage,
            SalesSupplyTypeId = i.SalesSupplyTypeId,
            SalesSupplyTypeName = i.SalesSupplyTypeName,
            SalesVatPercentage = i.SalesVatPercentage,
            SdPercentage = i.SdPercentage,
            IsVatInclusive = i.IsVatInclusive,
            IsVatRebatable = i.IsVatRebatable,
            IsSaleable = i.IsSaleable,
            IsPurchase = i.IsPurchase,
            IsNegativeSales = i.IsNegativeSales,
            IsMultipleSalesPrice = i.IsMultipleSalesPrice,
            ImageSource = i.ImageSource,
            IsActive = i.IsActive,
            CreatedAt = i.CreatedAt,
            CreatedByName = db.ErpUsers.Where(u => u.Id == i.CreatedById).Select(u => u.FullName).FirstOrDefault(),
            ModifiedAt = i.ModifiedAt,
            ModifiedByName = db.ErpUsers.Where(u => u.Id == i.ModifiedById).Select(u => u.FullName).FirstOrDefault(),
        });

    private static async Task<ItemList> List(
        string? search,
        string? status,
        string? sort,
        int? page,
        int? pageSize,
        int? departmentId,
        int? categoryId,
        int? itemTypeId,
        ErpDbContext db,
        CancellationToken ct)
    {
        var query = db.Items.AsNoTracking();
        if (departmentId is not null) query = query.Where(i => i.DepartmentId == departmentId);
        if (categoryId is not null) query = query.Where(i => i.CategoryId == categoryId);
        if (itemTypeId is not null) query = query.Where(i => i.ItemTypeId == itemTypeId);
        if (MasterData.Search(search) is { } term)
            query = query.Where(i => i.Code.Contains(term) || i.Name.Contains(term) || i.BarCode.Contains(term) || i.HsCode.Contains(term));

        var counts = await query.CountByStatusAsync(ct);
        query = query.WithStatus(status);

        var sorted = sort switch
        {
            "code" => query.OrderBy(i => i.Code),
            "-code" => query.OrderByDescending(i => i.Code),
            "-name" => query.OrderByDescending(i => i.Name),
            "salesPrice" => query.OrderBy(i => i.SalesPrice),
            "-salesPrice" => query.OrderByDescending(i => i.SalesPrice),
            "modifiedAt" => query.OrderBy(i => i.ModifiedAt ?? i.CreatedAt),
            "-modifiedAt" => query.OrderByDescending(i => i.ModifiedAt ?? i.CreatedAt),
            _ => query.OrderBy(i => i.Name),
        };

        var items = await Rows(sorted.ThenBy(i => i.Id).Page(page, pageSize)).ToListAsync(ct);
        return new ItemList(items, counts.TotalFor(status), counts);
    }

    private static ItemChoiceLists Choices() => new(ItemChoices.ItemTypes, ItemChoices.SupplyTypes);

    private static Task<List<ItemLookup>> Options(string? search, ErpDbContext db, CancellationToken ct)
    {
        var query = db.Items.AsNoTracking().Where(i => i.IsActive && !i.IsChildBarcode);
        if (MasterData.Search(search) is { } term)
            query = query.Where(i => i.Code.Contains(term) || i.Name.Contains(term) || i.BarCode.Contains(term));

        return query.OrderBy(i => i.Name)
            .Take(OptionLimit)
            .Select(i => new ItemLookup(i.Id, i.Code, i.Name, i.IsActive, null))
            .ToListAsync(ct);
    }

    private static async Task<Results<Ok<ItemDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken ct) =>
        await Details(db, id).SingleOrDefaultAsync(ct) is { } item
            ? TypedResults.Ok(item)
            : TypedResults.NotFound();

    private static async Task<Results<Created<ItemDetail>, ValidationProblem, ProblemHttpResult>> Create(
        SaveItemRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var (accountId, businessUnitId) = current.Scope();
        var (configured, errors) = await configs.ApplyRulesAsync(accountId, Feature, request, null, ct);
        if (MasterData.Invalid(errors) is { } rejected) return rejected;

        var item = new Item { AccountId = accountId, BusinessUnitId = businessUnitId };
        if (await ApplyAsync(db, item, configured, ct) is { } invalid) return invalid;

        db.Items.Add(item);
        if (await SaveAsync(db, ct) is { } problem) return problem;

        return TypedResults.Created($"/api/erp/item-management/items/{item.Id}", await Details(db, item.Id).SingleAsync(ct));
    }

    private static async Task<Results<Ok<ItemDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveItemRequest request, ErpDbContext db, ICurrentContext current, FeatureConfigService configs, CancellationToken ct)
    {
        var existing = await Details(db, id).SingleOrDefaultAsync(ct);
        var item = await db.Items.SingleOrDefaultAsync(i => i.Id == id, ct);
        if (existing is null || item is null) return TypedResults.NotFound();

        var (configured, errors) = await configs.ApplyRulesAsync(current.Scope().AccountId, Feature, request, existing, ct);
        if (MasterData.Invalid(errors) is { } rejected) return rejected;
        if (await ApplyAsync(db, item, configured, ct) is { } invalid) return invalid;
        if (await SaveAsync(db, ct) is { } problem) return problem;

        return TypedResults.Ok(await Details(db, id).SingleAsync(ct));
    }

    private static Task<Results<Ok<AffectedRecords>, NotFound>> SetStatus(
        RecordStatusRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.SetStatusAsync<Item>(db, request, ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> Delete(
        int id, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<Item>(db, [id], (ids, token) => UsageAsync(db, ids, token), ct);

    private static Task<Results<Ok<AffectedRecords>, NotFound, ProblemHttpResult>> DeleteMany(
        RecordIdsRequest request, ErpDbContext db, CancellationToken ct) =>
        MasterData.DeleteAsync<Item>(db, request.Ids, (ids, token) => UsageAsync(db, ids, token), ct);

    private static async Task<ValidationProblem?> ApplyAsync(
        ErpDbContext db, Item item, SaveItemRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        var minor = await db.ItemMinorCategories.Where(m => m.Id == request.MinorCategoryId)
            .Select(m => new { m.SubCategoryId, m.CategoryId, m.Category!.DepartmentId, CategoryCode = m.Category.Code, m.IsActive })
            .SingleOrDefaultAsync(ct);
        if (minor is null)
            errors["minorCategoryId"] = ["Choose a minor category of this business unit."];
        else if (!minor.IsActive && request.MinorCategoryId != item.MinorCategoryId)
            errors["minorCategoryId"] = ["This minor category is inactive. Activate it or choose another."];

        var units = await db.UnitsOfMeasure.AsNoTracking()
            .Where(u => u.Id == request.UomId || u.Id == request.AltUomId)
            .ToListAsync(ct);
        var uom = units.Find(u => u.Id == request.UomId);
        var altUom = units.Find(u => u.Id == request.AltUomId);
        if (UnitError(uom, item.UomId) is { } uomError) errors["uomId"] = [uomError];
        if (request.AltUomId is not null && UnitError(altUom, item.AltUomId) is { } altUomError) errors["altUomId"] = [altUomError];

        if (request.IsChildBarcode && request.MasterItemId is { } masterItemId)
        {
            var master = await db.Items.Where(i => i.Id == masterItemId)
                .Select(i => new { i.IsChildBarcode, i.IsActive })
                .SingleOrDefaultAsync(ct);
            if (master is null || masterItemId == item.Id)
                errors["masterItemId"] = ["Choose another item of this business unit."];
            else if (master.IsChildBarcode)
                errors["masterItemId"] = ["This item is a child item itself. Choose its master item."];
            else if (!master.IsActive && masterItemId != item.MasterItemId)
                errors["masterItemId"] = ["This item is inactive. Activate it or choose another."];
            else if (item.Id > 0 && await db.Items.AnyAsync(i => i.MasterItemId == item.Id, ct))
                errors["isChildBarcode"] = ["Other items use this item as their master item, so it cannot be a child item."];
        }

        var code = Trimmed(request.Code) ?? (item.Id > 0 ? item.Code : null);
        if (code is not null && await db.Items.AnyAsync(i => i.Id != item.Id && i.Code == code, ct))
            errors["code"] = ["Another item already uses this code."];

        var barCode = request.IsAutoBarcode ? null : Trimmed(request.BarCode);
        if (barCode is not null && await db.Items.AnyAsync(i => i.Id != item.Id && i.BarCode == barCode, ct))
            errors["barCode"] = ["Another item already uses this barcode."];

        if (MasterData.Invalid(errors) is { } invalid) return invalid;

        item.Code = code ?? await NextCodeAsync(db, minor!.CategoryCode, minor.CategoryId, ct);
        item.Name = request.Name.Trim();
        item.Description = Trimmed(request.Description);
        item.ItemTypeId = request.ItemTypeId;
        item.ItemTypeName = ItemChoices.NameOf(ItemChoices.ItemTypes, request.ItemTypeId);
        item.HsCode = request.HsCode?.Trim() ?? "";
        item.DepartmentId = minor!.DepartmentId;
        item.CategoryId = minor.CategoryId;
        item.SubCategoryId = minor.SubCategoryId;
        item.MinorCategoryId = request.MinorCategoryId;
        item.SupplierName = Trimmed(request.SupplierName);
        item.UomId = request.UomId;
        item.UomName = uom!.Name;
        item.AltUomId = altUom?.Id;
        item.AltUomName = altUom?.Name;
        item.AltUomConversionQty = altUom is null ? null : request.AltUomConversionQty;
        item.IsNoDecimalQty = request.IsNoDecimalQty;
        item.AverageCostPrice = request.AverageCostPrice;
        item.LastCostPrice = request.LastCostPrice;
        item.SalesPrice = request.SalesPrice;
        item.PurchaseSupplyTypeId = request.PurchaseSupplyTypeId;
        item.PurchaseSupplyTypeName = ItemChoices.NameOf(ItemChoices.SupplyTypes, request.PurchaseSupplyTypeId);
        item.PurchaseVatPercentage = request.PurchaseVatPercentage;
        item.SalesSupplyTypeId = request.SalesSupplyTypeId;
        item.SalesSupplyTypeName = ItemChoices.NameOf(ItemChoices.SupplyTypes, request.SalesSupplyTypeId);
        item.SalesVatPercentage = request.SalesVatPercentage;
        item.SdPercentage = request.SdPercentage;
        item.IsVatInclusive = request.IsVatInclusive;
        item.IsVatRebatable = request.IsVatRebatable;
        item.IsSaleable = request.IsSaleable;
        item.IsPurchase = request.IsPurchase;
        item.IsNegativeSales = request.IsNegativeSales;
        item.IsMultipleSalesPrice = request.IsMultipleSalesPrice;
        item.IsChildBarcode = request.IsChildBarcode;
        item.MasterItemId = request.IsChildBarcode ? request.MasterItemId : null;
        item.ImageSource = MasterData.CleanImage(request.ImageSource);
        item.IsActive = request.IsActive;
        await SetBarCodeAsync(db, item, request.IsAutoBarcode, barCode, ct);
        return null;
    }

    private static string? UnitError(UnitOfMeasure? unit, int? currentId) =>
        unit is null ? "Choose a unit of measure of this account."
        : !unit.IsActive && unit.Id != currentId ? "This unit is inactive. Activate it or choose another."
        : null;

    private static async Task SetBarCodeAsync(ErpDbContext db, Item item, bool auto, string? barCode, CancellationToken ct)
    {
        var keepGenerated = auto && item.IsAutoBarcode && item.BarCodeSerial is not null;
        item.IsAutoBarcode = auto;
        if (!auto) item.BarCode = barCode!;
        else if (!keepGenerated) (item.BarCodeSerial, item.BarCode) = await NextBarCodeAsync(db, ct);
    }

    private static async Task<(int Serial, string BarCode)> NextBarCodeAsync(ErpDbContext db, CancellationToken ct)
    {
        var serial = await db.Items.MaxAsync(i => i.BarCodeSerial, ct) ?? 0;
        string barCode;
        do barCode = Ean13(++serial);
        while (await db.Items.AnyAsync(i => i.BarCode == barCode, ct));
        return (serial, barCode);
    }

    private static string Ean13(int serial)
    {
        var digits = $"20{serial:D10}";
        var sum = digits.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return $"{digits}{(10 - sum % 10) % 10}";
    }

    private static async Task<string> NextCodeAsync(ErpDbContext db, string prefix, int categoryId, CancellationToken ct)
    {
        var number = await db.Items.CountAsync(i => i.CategoryId == categoryId, ct);
        string code;
        do code = $"{prefix}-{++number:D5}";
        while (await db.Items.AnyAsync(i => i.Code == code, ct));
        return code;
    }

    private static async Task<ProblemHttpResult?> SaveAsync(ErpDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return null;
        }
        catch (DbUpdateException e) when (MasterData.IsUniqueViolation(e))
        {
            return MasterData.Duplicate();
        }
    }

    private static async Task<string?> UsageAsync(ErpDbContext db, List<int> ids, CancellationToken ct)
    {
        var used = await db.Items
            .Where(i => ids.Contains(i.Id))
            .Select(i => new { i.Name, Children = db.Items.Count(c => c.MasterItemId == i.Id) })
            .Where(x => x.Children > 0)
            .FirstOrDefaultAsync(ct);

        return used is null
            ? null
            : $"“{used.Name}” is the master item of {MasterData.Plural(used.Children, "item", "items")}. Change them first, or deactivate it instead.";
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
