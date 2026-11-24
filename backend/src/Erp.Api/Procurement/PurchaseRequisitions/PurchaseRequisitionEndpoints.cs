using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.PartnerManagement;
using Erp.Api.UserManagement.Employees;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Procurement;
using Erp.Core.Modules;
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

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var requisitions = group.MapGroup("/purchase-requisitions");

        requisitions.MapGet("", List)
            .WithName("ListPurchaseRequisitions")
            .WithSummary("Purchase requisitions of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

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

        requisitions.MapGet("/{id:int}/approval", Approval)
            .WithName("GetPurchaseRequisitionApproval")
            .WithSummary("Approval steps of a requisition and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        requisitions.MapPost("/{id:int}/approve", Approve)
            .WithName("ApprovePurchaseRequisition")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectPurchaseRequisition")
            .WithSummary("Reject a pending requisition; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapPost("/{id:int}/close", Close)
            .WithName("ClosePurchaseRequisition")
            .WithSummary("Close an approved requisition so nothing more is bought against it")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelPurchaseRequisition")
            .WithSummary("Cancel an approved requisition; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requisitions.MapDelete("/{id:int}", Delete)
            .WithName("DeletePurchaseRequisition")
            .WithSummary("Delete a pending or rejected requisition")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

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
        if (DocumentData.Trimmed(search) is { } term)
        {
            requisitions = requisitions.Where(r => r.Code.Contains(term) ||
                (r.Remarks != null && r.Remarks.Contains(term)) ||
                (r.ProjectName != null && r.ProjectName.Contains(term)) ||
                r.Rows.Any(l => l.Item!.Name.Contains(term) || l.Item.Code.Contains(term)));
        }

        if (warehouseId is not null) requisitions = requisitions.Where(r => r.WarehouseId == warehouseId);
        if (requestTypeId is not null) requisitions = requisitions.Where(r => r.RequestTypeId == requestTypeId);
        if (departmentId is not null) requisitions = requisitions.Where(r => r.DepartmentId == departmentId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(requisitions, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(r => r.Code),
            "-code" => filtered.OrderByDescending(r => r.Code),
            "requestDate" => filtered.OrderBy(r => r.RequestDate).ThenBy(r => r.Id),
            "dueDate" => filtered.OrderBy(r => r.DueDate),
            "-dueDate" => filtered.OrderByDescending(r => r.DueDate),
            _ => filtered.OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
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
                r.DepartmentId, r.DepartmentName, r.EmployeeId, r.EmployeeName, ApprovalFlow.StatusKey(r.Status), r.ItemCount,
                r.TotalQuantity, r.TotalAmount, r.CreatedAt, r.ModifiedAt)).ToList(),
            total,
            counts);
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
        if (requisition.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(requisition, request, current, db, configs, cancellationToken);
    }

    private static Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, r => ApprovalFlow.ActAsync(db, current, Feature, r, true, request.Comment, cancellationToken), cancellationToken);

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, r => ApprovalFlow.ActAsync(db, current, Feature, r, false, request.Comment, cancellationToken),
                cancellationToken);

    private static Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Close(
        int id, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, r => Task.FromResult(DocumentData.Close(r)), cancellationToken);

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var requisition = await db.PurchaseRequisitions.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        return requisition is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, requisition, cancellationToken));
    }

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, 
                d => ApprovalFlow.CancelAsync(db, current, Feature, d, request.Comment,
                    () => PurchaseCancel.RequisitionAsync(db, current, d, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var requisition = await db.PurchaseRequisitions.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (requisition is null) return TypedResults.NotFound();
        if (DocumentData.Delete(requisition) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<PurchaseRequisition, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var requisition = await db.PurchaseRequisitions.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (requisition is null) return TypedResults.NotFound();
        if (await change(requisition) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<RequisitionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        PurchaseRequisition requisition, SaveRequisitionRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = requisition.Id > 0 ? ToRequest(requisition) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await DocumentData.CodeAsync(db.PurchaseRequisitions, requisition, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("requestTypeId", request.RequestTypeId, existing?.RequestTypeId, ProcurementLookups.RequestType),
            ("purchaseMethodId", request.PurchaseMethodId, existing?.PurchaseMethodId, ProcurementLookups.PurchaseMethod),
            ("itemTypeId", request.ItemTypeId, existing?.ItemTypeId, ItemLookups.ItemType),
            ("departmentId", request.DepartmentId, existing?.DepartmentId, EmployeeLookups.Department),
            ("sectionId", request.SectionId, existing?.SectionId, EmployeeLookups.DepartmentSection),
        ], errors, cancellationToken);
        await DocumentData.CheckWarehouseAsync(db, request.WarehouseId, existing?.WarehouseId, errors, cancellationToken);
        await CheckPeopleAsync(db, request, existing, errors, cancellationToken);
        var items = await DocumentData.CheckItemsAsync(
            db, [.. request.Lines.Select(l => l.ItemId)], requisition.Rows.Select(r => r.ItemId), errors, cancellationToken);

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        if (requisition.Id == 0) db.PurchaseRequisitions.Add(requisition);
        requisition.Code = code ?? await DocumentData.NextCodeAsync(db.PurchaseRequisitions, CodePrefix, cancellationToken);
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
        requisition.ProjectName = DocumentData.Trimmed(request.ProjectName);
        requisition.Remarks = DocumentData.Trimmed(request.Remarks);
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
                Remarks = DocumentData.Trimmed(line.Remarks),
            });
        }

        requisition.TotalQuantity = requisition.Rows.Sum(r => r.Quantity);
        requisition.TotalAmount = requisition.Rows.Sum(r => r.Amount);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, requisition.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, requisition.Id, cancellationToken))!);
    }

    private static async Task CheckPeopleAsync(
        ErpDbContext db, SaveRequisitionRequest request, SaveRequisitionRequest? existing, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        foreach (var (field, id, current) in new[]
                 {
                     ("employeeId", request.EmployeeId, existing?.EmployeeId),
                     ("buyerId", request.BuyerId, existing?.BuyerId),
                 })
        {
            if (id is not null && id != current && !await db.Employees.AnyAsync(e => e.Id == id && e.IsActive, cancellationToken))
            {
                errors[field] = [DocumentData.ChooseFromList];
            }
        }
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
            Status = ApprovalFlow.StatusKey(r.Status),
            ReviewedAt = r.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, r.ReviewedById, cancellationToken),
            ReviewComment = r.ReviewComment,
            TotalQuantity = r.TotalQuantity,
            TotalAmount = r.TotalAmount,
            Lines = [.. r.Rows.OrderBy(l => l.Sequence).Select(l => new RequisitionLine(
                l.Id, l.ItemId, l.Item!.Code, l.Item.Name, l.UomId, l.Uom!.Name, l.Quantity, l.OrderedQuantity, l.Rate, l.Amount,
                l.Remarks))],
            CustomData = CustomFieldData.Parse(r.CustomData),
            CreatedAt = r.CreatedAt,
            ModifiedAt = r.ModifiedAt,
        };
    }
}
