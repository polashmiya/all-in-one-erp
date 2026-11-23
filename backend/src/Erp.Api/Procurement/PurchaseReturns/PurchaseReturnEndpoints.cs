using Erp.Api.Approvals;
using Erp.Api.PartnerManagement;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Procurement;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchaseReturns;

internal static class PurchaseReturnEndpoints
{
    private const string Feature = ProcurementMenu.PurchaseReturn;
    private const string CodePrefix = "RTN-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var returns = group.MapGroup("/purchase-returns");

        returns.MapGet("", List)
            .WithName("ListPurchaseReturns")
            .WithSummary("Goods returned to suppliers in the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        returns.MapGet("/receives", Receives)
            .WithName("GetReturnReceiveOptions")
            .WithSummary("Approved goods receives of the selected business unit with the quantity that can still go back")
            .RequireAnyPermission(Editors);

        returns.MapGet("/{id:int}", Get)
            .WithName("GetPurchaseReturn")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        returns.MapPost("", Create)
            .WithName("CreatePurchaseReturn")
            .WithSummary("Return goods of an approved receive; an empty number gets the next RTN- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SavePurchaseReturnRequest>();

        returns.MapPut("/{id:int}", Update)
            .WithName("UpdatePurchaseReturn")
            .WithSummary("Change a pending return")
            .RequirePermission($"{Feature}.edit")
            .Validate<SavePurchaseReturnRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        returns.MapGet("/{id:int}/approval", Approval)
            .WithName("GetPurchaseReturnApproval")
            .WithSummary("Approval steps of a return and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        returns.MapPost("/{id:int}/approve", Approve)
            .WithName("ApprovePurchaseReturn")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        returns.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectPurchaseReturn")
            .WithSummary("Reject a pending return; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        returns.MapDelete("/{id:int}", Delete)
            .WithName("DeletePurchaseReturn")
            .WithSummary("Delete a pending or rejected return")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<PurchaseReturnList> List(
        string? search,
        string? status,
        int? supplierId,
        Guid? warehouseId,
        int? reasonId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var returns = db.PurchaseReturns.AsNoTracking().Where(r => r.IsActive);
        if (ProcurementData.Trimmed(search) is { } term)
        {
            returns = returns.Where(r => r.Code.Contains(term) ||
                (r.Remarks != null && r.Remarks.Contains(term)) ||
                db.PurchaseReceives.Any(g => g.Id == r.PurchaseReceiveId && g.Code.Contains(term)) ||
                db.Suppliers.Any(s => s.Id == r.SupplierId && s.Name.Contains(term)) ||
                r.Lines.Any(l => l.Item!.Name.Contains(term) || l.Item.Code.Contains(term)));
        }

        if (supplierId is not null) returns = returns.Where(r => r.SupplierId == supplierId);
        if (warehouseId is not null) returns = returns.Where(r => r.WarehouseId == warehouseId);
        if (reasonId is not null) returns = returns.Where(r => r.ReasonId == reasonId);

        var (filtered, counts) = await ProcurementData.ByStatusAsync(returns, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(r => r.Code),
            "-code" => filtered.OrderByDescending(r => r.Code),
            "returnDate" => filtered.OrderBy(r => r.ReturnDate).ThenBy(r => r.Id),
            _ => filtered.OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id),
        };

        var rows = await ProcurementData.Page(sorted, page, pageSize)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.ReturnDate,
                r.PurchaseReceiveId,
                PurchaseReceiveName = db.PurchaseReceives.Where(g => g.Id == r.PurchaseReceiveId).Select(g => g.Code).FirstOrDefault(),
                r.SupplierId,
                SupplierName = db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefault(),
                r.WarehouseId,
                WarehouseName = db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                r.ReasonId,
                ReasonName = db.LookupValues.Where(v => v.Id == r.ReasonId).Select(v => v.NameEn).FirstOrDefault(),
                r.Status,
                ItemCount = r.Lines.Count,
                r.TotalQuantity,
                r.NetAmount,
                r.CreatedAt,
                r.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new PurchaseReturnList(
            rows.Select(r => new PurchaseReturnRow(
                r.Id, r.Code, r.ReturnDate, r.PurchaseReceiveId, r.PurchaseReceiveName, r.SupplierId, r.SupplierName, r.WarehouseId,
                r.WarehouseName, r.ReasonId, r.ReasonName, ApprovalFlow.StatusKey(r.Status), r.ItemCount, r.TotalQuantity,
                r.NetAmount, r.CreatedAt, r.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<List<ReturnReceiveOption>> Receives(ErpDbContext db, CancellationToken cancellationToken)
    {
        var receives = await db.PurchaseReceives.AsNoTracking()
            .Where(r => r.IsActive && r.Status == ApprovalStatus.Approved)
            .OrderByDescending(r => r.ReceiveDate).ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.SupplierId,
                SupplierName = db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefault(),
                r.WarehouseId,
                r.IsVatInclusive,
                Lines = r.Lines.Where(l => l.Quantity > l.ReturnedQuantity).OrderBy(l => l.Sequence).Select(l => new ReturnReceiveLine(
                    l.ItemId, l.Item!.Code, l.Item.Name, l.Uom!.Name, l.Quantity, l.ReturnedQuantity, l.Quantity - l.ReturnedQuantity,
                    l.Price, l.Quantity > 0 ? Math.Round(l.Discount / l.Quantity, 6) : 0, l.VatPercentage, l.Batch)).ToList(),
            })
            .ToListAsync(cancellationToken);

        return [.. receives
            .Where(r => r.Lines.Count > 0)
            .Select(r => new ReturnReceiveOption(
                r.Id, r.Code, r.Code, r.SupplierId, r.SupplierName, r.WarehouseId, r.IsVatInclusive, true, r.Lines))];
    }

    private static async Task<Results<Ok<PurchaseReturnDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<PurchaseReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SavePurchaseReturnRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var purchaseReturn = new PurchaseReturn
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(purchaseReturn, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<PurchaseReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SavePurchaseReturnRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var purchaseReturn = await db.PurchaseReturns.Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (purchaseReturn is null) return TypedResults.NotFound();
        if (purchaseReturn.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(purchaseReturn, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var purchaseReturn = await db.PurchaseReturns.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        return purchaseReturn is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, purchaseReturn, cancellationToken));
    }

    private static Task<Results<Ok<PurchaseReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, r => ApprovalFlow.ActAsync(db, current, Feature, r, true, request.Comment, cancellationToken), cancellationToken);

    private static async Task<Results<Ok<PurchaseReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, r => ApprovalFlow.ActAsync(db, current, Feature, r, false, request.Comment, cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var purchaseReturn = await db.PurchaseReturns.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (purchaseReturn is null) return TypedResults.NotFound();
        if (ProcurementData.Delete(purchaseReturn) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await ReceiveReturns.SyncAsync(db, [purchaseReturn.PurchaseReceiveId], cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<PurchaseReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<PurchaseReturn, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var purchaseReturn = await db.PurchaseReturns.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (purchaseReturn is null) return TypedResults.NotFound();
        if (await change(purchaseReturn) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        await ReceiveReturns.SyncAsync(db, [purchaseReturn.PurchaseReceiveId], cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<PurchaseReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        PurchaseReturn purchaseReturn, SavePurchaseReturnRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = purchaseReturn.Id > 0 ? ToRequest(purchaseReturn) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await ProcurementData.CodeAsync(db.PurchaseReturns, purchaseReturn, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
            [("reasonId", request.ReasonId, existing?.ReasonId, ProcurementLookups.ReturnReason)], errors, cancellationToken);
        await ProcurementData.CheckWarehouseAsync(db, request.WarehouseId, existing?.WarehouseId, errors, cancellationToken);

        var currentReceive = existing?.PurchaseReceiveId;
        var receive = await db.PurchaseReceives.AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .SingleOrDefaultAsync(r => r.Id == request.PurchaseReceiveId && r.IsActive &&
                (r.Status == ApprovalStatus.Approved || r.Id == currentReceive), cancellationToken);
        if (receive is null) errors["purchaseReceiveId"] = ["Choose an approved goods receive of this business unit."];
        if (request.Lines.Select(l => l.ItemId).Distinct().Count() != request.Lines.Count) errors["lines"] = ["Each item can be added only once."];
        if (errors.Count > 0 || receive is null) return TypedResults.ValidationProblem(errors);

        var received = receive.Lines.ToDictionary(l => l.ItemId);
        var returned = await ReceiveReturns.ReturnedAsync(db, [receive.Id], purchaseReturn.Id, cancellationToken);
        var problems = new List<string>();
        foreach (var line in request.Lines)
        {
            if (!received.TryGetValue(line.ItemId, out var receiveLine))
            {
                problems.Add("Every item must be on the goods receive.");
                continue;
            }

            var left = receiveLine.Quantity - returned.GetValueOrDefault((receive.Id, line.ItemId));
            if (line.Quantity > left) problems.Add($"Only {Math.Max(0, left):0.####} of {receiveLine.Item!.Code} can be returned.");
        }

        if (problems.Count > 0) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["lines"] = [.. problems.Distinct()] });

        var lines = request.Lines.Select((line, index) =>
        {
            var receiveLine = received[line.ItemId];
            var result = new PurchaseReturnLine
            {
                Sequence = index + 1,
                ItemId = line.ItemId,
                UomId = receiveLine.UomId,
                Quantity = line.Quantity,
                Price = line.Price ?? receiveLine.Price,
                Discount = line.Discount ?? 0,
                VatPercentage = line.VatPercentage ?? receiveLine.VatPercentage,
                Batch = ProcurementData.Trimmed(line.Batch) ?? receiveLine.Batch,
            };
            (result.TotalAmount, result.VatAmount, result.NetAmount) =
                Pricing.Line(result.Quantity, result.Price, result.Discount, result.VatPercentage, request.IsVatInclusive);
            return result;
        }).ToList();

        if (lines.Any(l => l.Discount > l.TotalAmount))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["lines"] = ["A discount cannot be more than the line amount."] });
        }

        var previousReceive = purchaseReturn.Id > 0 ? purchaseReturn.PurchaseReceiveId : (int?)null;
        purchaseReturn.Lines.Clear();
        purchaseReturn.Lines.AddRange(lines);
        purchaseReturn.TotalQuantity = lines.Sum(l => l.Quantity);
        purchaseReturn.TotalVatAmount = lines.Sum(l => l.VatAmount);
        purchaseReturn.ItemTotalAmount = lines.Sum(l => l.NetAmount);
        purchaseReturn.NetAmount = purchaseReturn.ItemTotalAmount;

        if (purchaseReturn.Id == 0) db.PurchaseReturns.Add(purchaseReturn);
        purchaseReturn.Code = code ?? await ProcurementData.NextCodeAsync(db.PurchaseReturns, CodePrefix, cancellationToken);
        purchaseReturn.ReturnDate = request.ReturnDate;
        purchaseReturn.PurchaseReceiveId = receive.Id;
        purchaseReturn.PurchaseOrderId = receive.PurchaseOrderId;
        purchaseReturn.SupplierId = receive.SupplierId;
        purchaseReturn.WarehouseId = request.WarehouseId;
        purchaseReturn.ReasonId = request.ReasonId;
        purchaseReturn.IsVatInclusive = request.IsVatInclusive;
        purchaseReturn.Remarks = ProcurementData.Trimmed(request.Remarks);
        purchaseReturn.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, purchaseReturn.Id, cancellationToken);
        await ReceiveReturns.SyncAsync(db, [previousReceive, purchaseReturn.PurchaseReceiveId], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, purchaseReturn.Id, cancellationToken))!);
    }

    private static SavePurchaseReturnRequest ToRequest(PurchaseReturn r) => new()
    {
        Code = r.Code,
        ReturnDate = r.ReturnDate,
        PurchaseReceiveId = r.PurchaseReceiveId,
        WarehouseId = r.WarehouseId,
        ReasonId = r.ReasonId,
        IsVatInclusive = r.IsVatInclusive,
        Remarks = r.Remarks,
        Lines = [.. r.Lines.OrderBy(l => l.Sequence).Select(l => new SavePurchaseReturnLine
        {
            ItemId = l.ItemId,
            Quantity = l.Quantity,
            Price = l.Price,
            Discount = l.Discount,
            VatPercentage = l.VatPercentage,
            Batch = l.Batch,
        })],
        CustomData = CustomFieldData.Parse(r.CustomData),
    };

    private static async Task<PurchaseReturnDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var r = await db.PurchaseReturns.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .Include(x => x.Lines).ThenInclude(l => l.Uom)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (r is null) return null;

        var names = await PartnerData.LookupNamesAsync(db, [r.ReasonId], cancellationToken);

        return new PurchaseReturnDetail
        {
            Id = r.Id,
            Code = r.Code,
            ReturnDate = r.ReturnDate,
            PurchaseReceiveId = r.PurchaseReceiveId,
            PurchaseReceiveName = await db.PurchaseReceives.Where(g => g.Id == r.PurchaseReceiveId).Select(g => g.Code)
                .FirstOrDefaultAsync(cancellationToken),
            PurchaseOrderId = r.PurchaseOrderId,
            PurchaseOrderName = await db.PurchaseOrders.Where(o => o.Id == r.PurchaseOrderId).Select(o => o.Code).FirstOrDefaultAsync(cancellationToken),
            SupplierId = r.SupplierId,
            SupplierName = await db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken),
            WarehouseId = r.WarehouseId,
            WarehouseName = await db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            ReasonId = r.ReasonId,
            ReasonName = r.ReasonId is { } reason ? names.GetValueOrDefault(reason) : null,
            IsVatInclusive = r.IsVatInclusive,
            Remarks = r.Remarks,
            Status = ApprovalFlow.StatusKey(r.Status),
            ReviewedAt = r.ReviewedAt,
            ReviewedByName = await ProcurementData.ReviewerNameAsync(db, r.ReviewedById, cancellationToken),
            ReviewComment = r.ReviewComment,
            TotalQuantity = r.TotalQuantity,
            ItemTotalAmount = r.ItemTotalAmount,
            TotalVatAmount = r.TotalVatAmount,
            NetAmount = r.NetAmount,
            Lines = [.. r.Lines.OrderBy(l => l.Sequence).Select(l => new PurchaseReturnLineDetail(
                l.Id, l.ItemId, l.Item!.Code, l.Item.Name, l.UomId, l.Uom!.Name, l.Quantity, l.Price, l.TotalAmount, l.Discount,
                l.VatPercentage, l.VatAmount, l.NetAmount, l.Batch))],
            CustomData = CustomFieldData.Parse(r.CustomData),
            CreatedAt = r.CreatedAt,
            ModifiedAt = r.ModifiedAt,
        };
    }
}
