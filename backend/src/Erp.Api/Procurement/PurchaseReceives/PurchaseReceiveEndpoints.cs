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

namespace Erp.Api.Procurement.PurchaseReceives;

internal static class PurchaseReceiveEndpoints
{
    private const string Feature = ProcurementMenu.PurchaseReceive;
    private const string CodePrefix = "GRN-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var receives = group.MapGroup("/purchase-receives");

        receives.MapGet("", List)
            .WithName("ListPurchaseReceives")
            .WithSummary("Goods received (GRN) in the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        receives.MapGet("/orders", Orders)
            .WithName("GetReceiveOrderOptions")
            .WithSummary("Approved purchase orders of the selected business unit with the quantity still to receive")
            .RequireAnyPermission(Editors);

        receives.MapGet("/{id:int}", Get)
            .WithName("GetPurchaseReceive")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        receives.MapPost("", Create)
            .WithName("CreatePurchaseReceive")
            .WithSummary("Receive goods against an approved purchase order; an empty number gets the next GRN- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SavePurchaseReceiveRequest>();

        receives.MapPut("/{id:int}", Update)
            .WithName("UpdatePurchaseReceive")
            .WithSummary("Change a pending receive")
            .RequirePermission($"{Feature}.edit")
            .Validate<SavePurchaseReceiveRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        receives.MapGet("/{id:int}/approval", Approval)
            .WithName("GetPurchaseReceiveApproval")
            .WithSummary("Approval steps of a receive and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        receives.MapPost("/{id:int}/approve", Approve)
            .WithName("ApprovePurchaseReceive")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        receives.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectPurchaseReceive")
            .WithSummary("Reject a pending receive; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        receives.MapDelete("/{id:int}", Delete)
            .WithName("DeletePurchaseReceive")
            .WithSummary("Delete a pending or rejected receive")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<PurchaseReceiveList> List(
        string? search,
        string? status,
        int? supplierId,
        Guid? warehouseId,
        int? paymentTypeId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var receives = db.PurchaseReceives.AsNoTracking().Where(r => r.IsActive);
        if (ProcurementData.Trimmed(search) is { } term)
        {
            receives = receives.Where(r => r.Code.Contains(term) ||
                (r.ChallanNo != null && r.ChallanNo.Contains(term)) ||
                db.PurchaseOrders.Any(o => o.Id == r.PurchaseOrderId && o.Code.Contains(term)) ||
                db.Suppliers.Any(s => s.Id == r.SupplierId && s.Name.Contains(term)) ||
                r.Lines.Any(l => l.Item!.Name.Contains(term) || l.Item.Code.Contains(term)));
        }

        if (supplierId is not null) receives = receives.Where(r => r.SupplierId == supplierId);
        if (warehouseId is not null) receives = receives.Where(r => r.WarehouseId == warehouseId);
        if (paymentTypeId is not null) receives = receives.Where(r => r.PaymentTypeId == paymentTypeId);

        var (filtered, counts) = await ProcurementData.ByStatusAsync(receives, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(r => r.Code),
            "-code" => filtered.OrderByDescending(r => r.Code),
            "receiveDate" => filtered.OrderBy(r => r.ReceiveDate).ThenBy(r => r.Id),
            _ => filtered.OrderByDescending(r => r.ReceiveDate).ThenByDescending(r => r.Id),
        };

        var rows = await ProcurementData.Page(sorted, page, pageSize)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.ReceiveDate,
                r.PurchaseOrderId,
                PurchaseOrderName = db.PurchaseOrders.Where(o => o.Id == r.PurchaseOrderId).Select(o => o.Code).FirstOrDefault(),
                r.SupplierId,
                SupplierName = db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefault(),
                r.WarehouseId,
                WarehouseName = db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                r.ChallanNo,
                r.Status,
                ItemCount = r.Lines.Count,
                r.TotalQuantity,
                r.NetAmount,
                r.CreatedAt,
                r.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new PurchaseReceiveList(
            rows.Select(r => new PurchaseReceiveRow(
                r.Id, r.Code, r.ReceiveDate, r.PurchaseOrderId, r.PurchaseOrderName, r.SupplierId, r.SupplierName, r.WarehouseId,
                r.WarehouseName, r.ChallanNo, ApprovalFlow.StatusKey(r.Status), r.ItemCount, r.TotalQuantity, r.NetAmount,
                r.CreatedAt, r.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<List<ReceiveOrderOption>> Orders(ErpDbContext db, CancellationToken cancellationToken)
    {
        var orders = await db.PurchaseOrders.AsNoTracking()
            .Where(o => o.IsActive && o.Status == ApprovalStatus.Approved)
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new
            {
                o.Id,
                o.Code,
                o.SupplierId,
                SupplierName = db.Suppliers.Where(s => s.Id == o.SupplierId).Select(s => s.Name).FirstOrDefault(),
                Tolerance = db.Suppliers.Where(s => s.Id == o.SupplierId).Select(s => s.Tolerance).FirstOrDefault() ?? 0,
                o.WarehouseId,
                o.IsVatInclusive,
                o.PaymentTypeId,
                o.PaymentDays,
                Lines = o.Lines.OrderBy(l => l.Sequence).Select(l => new
                {
                    l.ItemId,
                    l.Item!.Code,
                    l.Item.Name,
                    Uom = l.Uom!.Name,
                    l.Quantity,
                    l.ReceivedQuantity,
                    l.Price,
                    l.Discount,
                    l.VatPercentage,
                }).ToList(),
            })
            .ToListAsync(cancellationToken);

        return [.. orders
            .Select(o => new ReceiveOrderOption(
                o.Id, o.Code, o.Code, o.SupplierId, o.SupplierName, o.WarehouseId, o.IsVatInclusive, o.PaymentTypeId, o.PaymentDays,
                true,
                [.. o.Lines
                    .Select(l => new ReceiveOrderLine(
                        l.ItemId, l.Code, l.Name, l.Uom, l.Quantity, l.ReceivedQuantity,
                        Math.Max(0, OrderReceipts.Allowed(l.Quantity, o.Tolerance) - l.ReceivedQuantity), l.Price,
                        l.Quantity > 0 ? Math.Round(l.Discount / l.Quantity, 6) : 0, l.VatPercentage))
                    .Where(l => l.Remaining > 0)]))
            .Where(o => o.Lines.Count > 0)];
    }

    private static async Task<Results<Ok<PurchaseReceiveDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<PurchaseReceiveDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SavePurchaseReceiveRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var receive = new PurchaseReceive
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(receive, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<PurchaseReceiveDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SavePurchaseReceiveRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var receive = await db.PurchaseReceives.Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (receive is null) return TypedResults.NotFound();
        if (receive.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(receive, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var receive = await db.PurchaseReceives.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        return receive is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, receive, cancellationToken));
    }

    private static Task<Results<Ok<PurchaseReceiveDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, r => ApprovalFlow.ActAsync(db, current, Feature, r, true, request.Comment, cancellationToken), cancellationToken);

    private static async Task<Results<Ok<PurchaseReceiveDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, r => ApprovalFlow.ActAsync(db, current, Feature, r, false, request.Comment, cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var receive = await db.PurchaseReceives.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (receive is null) return TypedResults.NotFound();
        if (ProcurementData.Delete(receive) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await OrderReceipts.SyncAsync(db, [receive.PurchaseOrderId], cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<PurchaseReceiveDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<PurchaseReceive, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var receive = await db.PurchaseReceives.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (receive is null) return TypedResults.NotFound();
        if (await change(receive) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        await OrderReceipts.SyncAsync(db, [receive.PurchaseOrderId], cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<PurchaseReceiveDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        PurchaseReceive receive, SavePurchaseReceiveRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = receive.Id > 0 ? ToRequest(receive) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await ProcurementData.CodeAsync(db.PurchaseReceives, receive, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
            [("paymentTypeId", request.PaymentTypeId, existing?.PaymentTypeId, ProcurementLookups.PaymentType)], errors, cancellationToken);
        await ProcurementData.CheckWarehouseAsync(db, request.WarehouseId, existing?.WarehouseId, errors, cancellationToken);

        var currentOrder = existing?.PurchaseOrderId;
        var order = await db.PurchaseOrders.AsNoTracking()
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .SingleOrDefaultAsync(o => o.Id == request.PurchaseOrderId && o.IsActive &&
                (o.Status == ApprovalStatus.Approved || o.Id == currentOrder), cancellationToken);
        if (order is null) errors["purchaseOrderId"] = ["Choose an approved purchase order of this business unit."];
        if (request.Lines.Select(l => l.ItemId).Distinct().Count() != request.Lines.Count) errors["lines"] = ["Each item can be added only once."];
        if (errors.Count > 0 || order is null) return TypedResults.ValidationProblem(errors);

        var ordered = order.Lines.ToDictionary(l => l.ItemId);
        var tolerance = await OrderReceipts.ToleranceAsync(db, order.Id, cancellationToken);
        var received = await OrderReceipts.ReceivedAsync(db, [order.Id], receive.Id, cancellationToken);
        var problems = new List<string>();
        foreach (var line in request.Lines)
        {
            if (!ordered.TryGetValue(line.ItemId, out var orderLine))
            {
                problems.Add("Every item must be on the purchase order.");
                continue;
            }

            var left = OrderReceipts.Allowed(orderLine.Quantity, tolerance) - received.GetValueOrDefault((order.Id, line.ItemId));
            if (line.Quantity > left) problems.Add($"Only {Math.Max(0, left):0.####} of {orderLine.Item!.Code} is left to receive.");
        }

        if (problems.Count > 0) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["lines"] = [.. problems.Distinct()] });

        var lines = request.Lines.Select((line, index) =>
        {
            var orderLine = ordered[line.ItemId];
            var result = new PurchaseReceiveLine
            {
                Sequence = index + 1,
                ItemId = line.ItemId,
                UomId = orderLine.UomId,
                Quantity = line.Quantity,
                Price = line.Price ?? orderLine.Price,
                Discount = line.Discount ?? 0,
                VatPercentage = line.VatPercentage ?? orderLine.VatPercentage,
                Batch = ProcurementData.Trimmed(line.Batch),
            };
            (result.TotalAmount, result.VatAmount, result.NetAmount) =
                Pricing.Line(result.Quantity, result.Price, result.Discount, result.VatPercentage, request.IsVatInclusive);
            return result;
        }).ToList();
        var itemTotal = lines.Sum(l => l.NetAmount);
        var headerDiscount = request.HeaderDiscount ?? 0;

        if (lines.Any(l => l.Discount > l.TotalAmount)) errors["lines"] = ["A discount cannot be more than the line amount."];
        else if (headerDiscount > itemTotal) errors["headerDiscount"] = ["The discount cannot be more than the item total."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var previousOrder = receive.Id > 0 ? receive.PurchaseOrderId : (int?)null;
        receive.Lines.Clear();
        receive.Lines.AddRange(lines);
        receive.HeaderDiscount = headerDiscount;
        receive.DeliveryCharge = request.DeliveryCharge ?? 0;
        receive.LaborCharge = request.LaborCharge ?? 0;
        receive.OtherCharge = request.OtherCharge ?? 0;
        receive.TotalQuantity = lines.Sum(l => l.Quantity);
        receive.TotalLineDiscount = lines.Sum(l => l.Discount);
        receive.TotalVatAmount = lines.Sum(l => l.VatAmount);
        receive.ItemTotalAmount = itemTotal;
        receive.NetAmount = itemTotal - receive.HeaderDiscount + receive.DeliveryCharge + receive.LaborCharge + receive.OtherCharge;

        if (receive.Id == 0) db.PurchaseReceives.Add(receive);
        receive.Code = code ?? await ProcurementData.NextCodeAsync(db.PurchaseReceives, CodePrefix, cancellationToken);
        receive.ReceiveDate = request.ReceiveDate;
        receive.PurchaseOrderId = order.Id;
        receive.SupplierId = order.SupplierId;
        receive.WarehouseId = request.WarehouseId;
        receive.ChallanNo = ProcurementData.Trimmed(request.ChallanNo);
        receive.ChallanDate = request.ChallanDate;
        receive.VatChallanNo = ProcurementData.Trimmed(request.VatChallanNo);
        receive.PaymentTypeId = request.PaymentTypeId;
        receive.PaymentDays = request.PaymentDays;
        receive.IsVatInclusive = request.IsVatInclusive;
        receive.Remarks = ProcurementData.Trimmed(request.Remarks);
        receive.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, receive.Id, cancellationToken);
        await OrderReceipts.SyncAsync(db, [previousOrder, receive.PurchaseOrderId], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, receive.Id, cancellationToken))!);
    }

    private static SavePurchaseReceiveRequest ToRequest(PurchaseReceive r) => new()
    {
        Code = r.Code,
        ReceiveDate = r.ReceiveDate,
        PurchaseOrderId = r.PurchaseOrderId,
        WarehouseId = r.WarehouseId,
        ChallanNo = r.ChallanNo,
        ChallanDate = r.ChallanDate,
        VatChallanNo = r.VatChallanNo,
        PaymentTypeId = r.PaymentTypeId,
        PaymentDays = r.PaymentDays,
        IsVatInclusive = r.IsVatInclusive,
        HeaderDiscount = r.HeaderDiscount,
        DeliveryCharge = r.DeliveryCharge,
        LaborCharge = r.LaborCharge,
        OtherCharge = r.OtherCharge,
        Remarks = r.Remarks,
        Lines = [.. r.Lines.OrderBy(l => l.Sequence).Select(l => new SavePurchaseReceiveLine
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

    private static async Task<PurchaseReceiveDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var r = await db.PurchaseReceives.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .Include(x => x.Lines).ThenInclude(l => l.Uom)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (r is null) return null;

        var names = await PartnerData.LookupNamesAsync(db, [r.PaymentTypeId], cancellationToken);

        return new PurchaseReceiveDetail
        {
            Id = r.Id,
            Code = r.Code,
            ReceiveDate = r.ReceiveDate,
            PurchaseOrderId = r.PurchaseOrderId,
            PurchaseOrderName = await db.PurchaseOrders.Where(o => o.Id == r.PurchaseOrderId).Select(o => o.Code).FirstOrDefaultAsync(cancellationToken),
            SupplierId = r.SupplierId,
            SupplierName = await db.Suppliers.Where(s => s.Id == r.SupplierId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken),
            WarehouseId = r.WarehouseId,
            WarehouseName = await db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            ChallanNo = r.ChallanNo,
            ChallanDate = r.ChallanDate,
            VatChallanNo = r.VatChallanNo,
            PaymentTypeId = r.PaymentTypeId,
            PaymentTypeName = r.PaymentTypeId is { } type ? names.GetValueOrDefault(type) : null,
            PaymentDays = r.PaymentDays,
            IsVatInclusive = r.IsVatInclusive,
            HeaderDiscount = r.HeaderDiscount,
            DeliveryCharge = r.DeliveryCharge,
            LaborCharge = r.LaborCharge,
            OtherCharge = r.OtherCharge,
            Remarks = r.Remarks,
            Status = ApprovalFlow.StatusKey(r.Status),
            ReviewedAt = r.ReviewedAt,
            ReviewedByName = await ProcurementData.ReviewerNameAsync(db, r.ReviewedById, cancellationToken),
            ReviewComment = r.ReviewComment,
            TotalQuantity = r.TotalQuantity,
            TotalLineDiscount = r.TotalLineDiscount,
            ItemTotalAmount = r.ItemTotalAmount,
            TotalVatAmount = r.TotalVatAmount,
            NetAmount = r.NetAmount,
            ReturnAmount = r.ReturnAmount,
            PaidAmount = r.PaidAmount,
            Lines = [.. r.Lines.OrderBy(l => l.Sequence).Select(l => new PurchaseReceiveLineDetail(
                l.Id, l.ItemId, l.Item!.Code, l.Item.Name, l.UomId, l.Uom!.Name, l.Quantity, l.ReturnedQuantity, l.Price,
                l.TotalAmount, l.Discount,
                l.VatPercentage, l.VatAmount, l.NetAmount, l.Batch))],
            CustomData = CustomFieldData.Parse(r.CustomData),
            CreatedAt = r.CreatedAt,
            ModifiedAt = r.ModifiedAt,
        };
    }
}
