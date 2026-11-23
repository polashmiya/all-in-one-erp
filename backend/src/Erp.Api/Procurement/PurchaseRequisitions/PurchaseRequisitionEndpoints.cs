using Erp.Api.PartnerManagement;
using Erp.Api.UserManagement.Employees;
using Erp.Core.Authorization;
using Erp.Core.Domain.Procurement;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement.Items;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchaseRequisitions;

internal static class PurchaseRequisitionEndpoints
{
    private const string Feature = ProcurementMenu.PurchaseRequisition;
    private const string CodePrefix = "PR-";
    private const int CodeDigits = 5;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 1000;
    private const int ItemOptionLimit = 20;
    private const string ChooseFromList = "Choose an active value from the list.";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var requisitions = group.MapGroup("/purchase-requisitions");

        requisitions.MapGet("", List)
            .WithName("ListPurchaseRequisitions")
            .WithSummary("Purchase requisitions of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        requisitions.MapGet("/items", Items)
            .WithName("GetRequisitionItemOptions")
            .WithSummary("Up to 20 active items matching the search, with their UOM and last cost")
            .RequireAnyPermission(Editors);

        requisitions.MapGet("/{id:int}", Get)
            .WithName("GetPurchaseRequisition")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        requisitions.MapPost("", Create)
            .WithName("CreatePurchaseRequisition")
            .WithSummary("Create a pending requisition; an empty number gets the next PR- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveRequisitionRequest>();

        requisitions.MapPut("/{id:int}", Update)
            .WithName("UpdatePurchaseRequisition")
            .WithSummary("Change a pending requisition")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveRequisitionRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapPost("/{id:int}/approve", Approve)
            .WithName("ApprovePurchaseRequisition")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequisitionRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectPurchaseRequisition")
            .WithSummary("Reject a pending requisition; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequisitionRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapPost("/{id:int}/close", Close)
            .WithName("ClosePurchaseRequisition")
            .WithSummary("Close an approved requisition so nothing more is bought against it")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapDelete("/{id:int}", Delete)
            .WithName("DeletePurchaseRequisition")
            .WithSummary("Delete a pending or rejected requisition")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    public static string StatusKey(RequisitionStatus status) => status.ToString().ToLowerInvariant();

    private static async Task<RequisitionList> List(
        string? search,
        string? status,
        Guid? warehouseId,
        int? requestTypeId,
        int? departmentId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var requisitions = db.PurchaseRequisitions.AsNoTracking().Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            requisitions = requisitions.Where(r => r.Code.Contains(term) ||
                (r.Remarks != null && r.Remarks.Contains(term)) ||
                (r.ProjectName != null && r.ProjectName.Contains(term)) ||
                r.Rows.Any(l => l.Item!.Name.Contains(term) || l.Item.Code.Contains(term)));
        }

        if (warehouseId is not null) requisitions = requisitions.Where(r => r.WarehouseId == warehouseId);
        if (requestTypeId is not null) requisitions = requisitions.Where(r => r.RequestTypeId == requestTypeId);
        if (departmentId is not null) requisitions = requisitions.Where(r => r.DepartmentId == departmentId);

        var byStatus = await requisitions.GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);
        int Count(RequisitionStatus s) => byStatus.GetValueOrDefault(s);
        var counts = new RequisitionCounts(
            byStatus.Values.Sum(),
            Count(RequisitionStatus.Pending),
            Count(RequisitionStatus.Approved),
            Count(RequisitionStatus.Rejected),
            Count(RequisitionStatus.Closed));

        if (Enum.TryParse<RequisitionStatus>(status, true, out var only)) requisitions = requisitions.Where(r => r.Status == only);

        var total = await requisitions.CountAsync(cancellationToken);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var sorted = sort switch
        {
            "code" => requisitions.OrderBy(r => r.Code),
            "-code" => requisitions.OrderByDescending(r => r.Code),
            "requestDate" => requisitions.OrderBy(r => r.RequestDate).ThenBy(r => r.Id),
            "dueDate" => requisitions.OrderBy(r => r.DueDate),
            "-dueDate" => requisitions.OrderByDescending(r => r.DueDate),
            "modifiedAt" => requisitions.OrderBy(r => r.ModifiedAt ?? r.CreatedAt),
            "-modifiedAt" => requisitions.OrderByDescending(r => r.ModifiedAt ?? r.CreatedAt),
            _ => requisitions.OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id),
        };

        var rows = await sorted.Skip((Math.Max(1, page ?? 1) - 1) * size).Take(size)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.RequestDate,
                r.DueDate,
                r.WarehouseId,
                WarehouseName = db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                r.RequestTypeId,
                RequestTypeName = db.LookupValues.Where(v => v.Id == r.RequestTypeId).Select(v => v.NameEn).FirstOrDefault(),
                r.DepartmentId,
                DepartmentName = db.LookupValues.Where(v => v.Id == r.DepartmentId).Select(v => v.NameEn).FirstOrDefault(),
                r.EmployeeId,
                EmployeeName = db.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.Name).FirstOrDefault(),
                r.Status,
                ItemCount = r.Rows.Count,
                r.TotalQuantity,
                r.TotalAmount,
                r.CreatedAt,
                r.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new RequisitionList(
            rows.Select(r => new RequisitionRow(
                r.Id, r.Code, r.RequestDate, r.DueDate, r.WarehouseId, r.WarehouseName, r.RequestTypeId, r.RequestTypeName,
                r.DepartmentId, r.DepartmentName, r.EmployeeId, r.EmployeeName, StatusKey(r.Status), r.ItemCount,
                r.TotalQuantity, r.TotalAmount, r.CreatedAt, r.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static Task<List<RequisitionItemOption>> Items(string? search, ErpDbContext db, CancellationToken cancellationToken)
    {
        var items = db.Items.AsNoTracking().Where(i => i.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            items = items.Where(i => i.Code.Contains(term) || i.Name.Contains(term) || i.BarCode.Contains(term));
        }

        return items.OrderBy(i => i.Name)
            .Take(ItemOptionLimit)
            .Select(i => new RequisitionItemOption(
                i.Id, i.Code, i.Name, i.UomId, i.Uom!.Name, i.LastCostPrice ?? i.AverageCostPrice ?? 0, i.IsActive))
            .ToListAsync(cancellationToken);
    }

    private static async Task<Results<Ok<RequisitionDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveRequisitionRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var requisition = new PurchaseRequisition
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(requisition, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveRequisitionRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var requisition = await db.PurchaseRequisitions.Include(r => r.Rows)
            .SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (requisition is null) return TypedResults.NotFound();
        if (requisition.Status != RequisitionStatus.Pending) return NotPending("changed");
        return await SaveAsync(requisition, request, current, db, configs, cancellationToken);
    }

    private static Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequisitionRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ReviewAsync(id, RequisitionStatus.Approved, request.Comment, current, db, cancellationToken);

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequisitionRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        if (PartnerData.Trimmed(request.Comment) is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["comment"] = ["Write why the requisition is rejected."] });
        }

        return await ReviewAsync(id, RequisitionStatus.Rejected, request.Comment, current, db, cancellationToken);
    }

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ReviewAsync(
        int id, RequisitionStatus decision, string? comment, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var requisition = await db.PurchaseRequisitions.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (requisition is null) return TypedResults.NotFound();
        if (requisition.Status != RequisitionStatus.Pending) return NotPending("reviewed");

        requisition.Status = decision;
        requisition.ReviewedAt = DateTime.UtcNow;
        requisition.ReviewedById = current.UserId;
        requisition.ReviewComment = PartnerData.Trimmed(comment);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ProblemHttpResult>> Close(
        int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var requisition = await db.PurchaseRequisitions.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (requisition is null) return TypedResults.NotFound();
        if (requisition.Status != RequisitionStatus.Approved)
        {
            return TypedResults.Problem(detail: "Only an approved requisition can be closed.", statusCode: StatusCodes.Status409Conflict);
        }

        requisition.Status = RequisitionStatus.Closed;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var requisition = await db.PurchaseRequisitions.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (requisition is null) return TypedResults.NotFound();
        if (requisition.Status is not (RequisitionStatus.Pending or RequisitionStatus.Rejected))
        {
            return TypedResults.Problem(detail: "Only a pending or rejected requisition can be deleted.", statusCode: StatusCodes.Status409Conflict);
        }

        requisition.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static ProblemHttpResult NotPending(string action) =>
        TypedResults.Problem(detail: $"Only a pending requisition can be {action}.", statusCode: StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        PurchaseRequisition requisition, SaveRequisitionRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = requisition.Id > 0 ? ToRequest(requisition) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await CodeAsync(db, requisition, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("requestTypeId", request.RequestTypeId, existing?.RequestTypeId, ProcurementLookups.RequestType),
            ("purchaseMethodId", request.PurchaseMethodId, existing?.PurchaseMethodId, ProcurementLookups.PurchaseMethod),
            ("itemTypeId", request.ItemTypeId, existing?.ItemTypeId, ItemLookups.ItemType),
            ("departmentId", request.DepartmentId, existing?.DepartmentId, EmployeeLookups.Department),
            ("sectionId", request.SectionId, existing?.SectionId, EmployeeLookups.DepartmentSection),
        ], errors, cancellationToken);
        await CheckPeopleAndPlaceAsync(db, request, existing, errors, cancellationToken);
        var items = await CheckLinesAsync(db, request.Lines, requisition, errors, cancellationToken);

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        if (requisition.Id == 0) db.PurchaseRequisitions.Add(requisition);
        requisition.Code = code ?? await NextCodeAsync(db, cancellationToken);
        requisition.RequestDate = request.RequestDate;
        requisition.DueDate = request.DueDate;
        requisition.WarehouseId = request.WarehouseId;
        requisition.RequestTypeId = request.RequestTypeId;
        requisition.PurchaseMethodId = request.PurchaseMethodId;
        requisition.ItemTypeId = request.ItemTypeId;
        requisition.DepartmentId = request.DepartmentId;
        requisition.SectionId = request.SectionId;
        requisition.EmployeeId = request.EmployeeId;
        requisition.BuyerId = request.BuyerId;
        requisition.ProjectName = PartnerData.Trimmed(request.ProjectName);
        requisition.Remarks = PartnerData.Trimmed(request.Remarks);
        requisition.CustomData = CustomFieldData.ToJson(request.CustomData);

        requisition.Rows.Clear();
        foreach (var (line, index) in request.Lines.Select((line, index) => (line, index)))
        {
            var item = items[line.ItemId];
            var rate = line.Rate ?? item.Rate;
            requisition.Rows.Add(new PurchaseRequisitionRow
            {
                Sequence = index + 1,
                ItemId = line.ItemId,
                UomId = item.UomId,
                Quantity = line.Quantity,
                Rate = rate,
                Amount = Math.Round(line.Quantity * rate, 2),
                Remarks = PartnerData.Trimmed(line.Remarks),
            });
        }

        requisition.TotalQuantity = requisition.Rows.Sum(r => r.Quantity);
        requisition.TotalAmount = requisition.Rows.Sum(r => r.Amount);

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, requisition.Id, cancellationToken))!);
    }

    private static async Task<string?> CodeAsync(
        ErpDbContext db, PurchaseRequisition requisition, string? sent, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        var code = PartnerData.Trimmed(sent)?.ToUpperInvariant() ?? (requisition.Id > 0 ? requisition.Code : null);
        if (code is not null && await db.PurchaseRequisitions.AnyAsync(r => r.Id != requisition.Id && r.Code == code, cancellationToken))
        {
            errors["code"] = ["Another requisition already uses this number."];
        }

        return code;
    }

    private static async Task<string> NextCodeAsync(ErpDbContext db, CancellationToken cancellationToken)
    {
        var codes = await db.PurchaseRequisitions.Where(r => r.Code.StartsWith(CodePrefix)).Select(r => r.Code).ToListAsync(cancellationToken);
        var last = codes.Select(c => int.TryParse(c[CodePrefix.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"{CodePrefix}{(last + 1).ToString($"D{CodeDigits}")}";
    }

    private static async Task CheckPeopleAndPlaceAsync(
        ErpDbContext db, SaveRequisitionRequest request, SaveRequisitionRequest? existing, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        if (request.WarehouseId != existing?.WarehouseId &&
            !await db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, cancellationToken))
        {
            errors["warehouseId"] = ["Choose an active warehouse of this business unit."];
        }

        foreach (var (field, id, current) in new[]
                 {
                     ("employeeId", request.EmployeeId, existing?.EmployeeId),
                     ("buyerId", request.BuyerId, existing?.BuyerId),
                 })
        {
            if (id is not null && id != current && !await db.Employees.AnyAsync(e => e.Id == id && e.IsActive, cancellationToken))
            {
                errors[field] = [ChooseFromList];
            }
        }
    }

    private static async Task<Dictionary<int, (int UomId, decimal Rate)>> CheckLinesAsync(
        ErpDbContext db, IReadOnlyList<SaveRequisitionLine> lines, PurchaseRequisition requisition, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var ids = lines.Select(l => l.ItemId).Distinct().ToList();
        var saved = requisition.Rows.Select(r => r.ItemId).ToHashSet();
        var items = await db.Items.AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .Select(i => new { i.Id, i.IsActive, i.UomId, Rate = i.LastCostPrice ?? i.AverageCostPrice ?? 0 })
            .ToListAsync(cancellationToken);

        var problems = new List<string>();
        if (ids.Count != lines.Count) problems.Add("Each item can be added only once.");
        if (items.Any(i => !i.IsActive && !saved.Contains(i.Id)) || items.Count != ids.Count)
        {
            problems.Add("Choose active items of this business unit.");
        }

        if (problems.Count > 0) errors["lines"] = [.. problems];
        return items.ToDictionary(i => i.Id, i => (i.UomId, i.Rate));
    }

    private static SaveRequisitionRequest ToRequest(PurchaseRequisition r) => new()
    {
        Code = r.Code,
        RequestDate = r.RequestDate,
        DueDate = r.DueDate,
        WarehouseId = r.WarehouseId,
        RequestTypeId = r.RequestTypeId,
        PurchaseMethodId = r.PurchaseMethodId,
        ItemTypeId = r.ItemTypeId,
        DepartmentId = r.DepartmentId,
        SectionId = r.SectionId,
        EmployeeId = r.EmployeeId,
        BuyerId = r.BuyerId,
        ProjectName = r.ProjectName,
        Remarks = r.Remarks,
        Lines = [.. r.Rows.OrderBy(l => l.Sequence).Select(l => new SaveRequisitionLine
        {
            ItemId = l.ItemId,
            Quantity = l.Quantity,
            Rate = l.Rate,
            Remarks = l.Remarks,
        })],
        CustomData = CustomFieldData.Parse(r.CustomData),
    };

    private static async Task<RequisitionDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var r = await db.PurchaseRequisitions.AsNoTracking()
            .Include(x => x.Rows).ThenInclude(l => l.Item)
            .Include(x => x.Rows).ThenInclude(l => l.Uom)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (r is null) return null;

        var names = await PartnerData.LookupNamesAsync(db,
            [r.RequestTypeId, r.PurchaseMethodId, r.ItemTypeId, r.DepartmentId, r.SectionId], cancellationToken);
        string? Lookup(int? key) => key is { } k ? names.GetValueOrDefault(k) : null;
        var people = await db.Employees.AsNoTracking()
            .Where(e => e.Id == r.EmployeeId || e.Id == r.BuyerId)
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
        string? Person(int? key) => key is { } k ? people.GetValueOrDefault(k) : null;

        return new RequisitionDetail
        {
            Id = r.Id,
            Code = r.Code,
            RequestDate = r.RequestDate,
            DueDate = r.DueDate,
            WarehouseId = r.WarehouseId,
            WarehouseName = await db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            RequestTypeId = r.RequestTypeId,
            RequestTypeName = Lookup(r.RequestTypeId),
            PurchaseMethodId = r.PurchaseMethodId,
            PurchaseMethodName = Lookup(r.PurchaseMethodId),
            ItemTypeId = r.ItemTypeId,
            ItemTypeName = Lookup(r.ItemTypeId),
            DepartmentId = r.DepartmentId,
            DepartmentName = Lookup(r.DepartmentId),
            SectionId = r.SectionId,
            SectionName = Lookup(r.SectionId),
            EmployeeId = r.EmployeeId,
            EmployeeName = Person(r.EmployeeId),
            BuyerId = r.BuyerId,
            BuyerName = Person(r.BuyerId),
            ProjectName = r.ProjectName,
            Remarks = r.Remarks,
            Status = StatusKey(r.Status),
            ReviewedAt = r.ReviewedAt,
            ReviewedByName = r.ReviewedById is { } reviewer
                ? await db.ErpUsers.Where(u => u.Id == reviewer).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken)
                : null,
            ReviewComment = r.ReviewComment,
            TotalQuantity = r.TotalQuantity,
            TotalAmount = r.TotalAmount,
            Lines = [.. r.Rows.OrderBy(l => l.Sequence).Select(l => new RequisitionLine(
                l.Id, l.ItemId, l.Item!.Code, l.Item.Name, l.UomId, l.Uom!.Name, l.Quantity, l.Rate, l.Amount, l.Remarks))],
            CustomData = CustomFieldData.Parse(r.CustomData),
            CreatedAt = r.CreatedAt,
            ModifiedAt = r.ModifiedAt,
        };
    }
}
