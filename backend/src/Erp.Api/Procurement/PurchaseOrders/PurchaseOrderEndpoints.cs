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
using Erp.Modules.Inventory.ItemManagement.Items;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchaseOrders;

internal static class PurchaseOrderEndpoints
{
    private const string Feature = ProcurementMenu.PurchaseOrder;
    private const string CodePrefix = "PO-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var orders = group.MapGroup("/purchase-orders");

        orders.MapGet("", List)
            .WithName("ListPurchaseOrders")
            .WithSummary("Purchase orders of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        orders.MapGet("/requisitions", Requisitions)
            .WithName("GetOrderRequisitionOptions")
            .WithSummary("Approved requisitions of the selected business unit with the quantity still to order")
            .RequireAnyPermission(Editors);

        orders.MapGet("/{id:int}", Get)
            .WithName("GetPurchaseOrder")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("", Create)
            .WithName("CreatePurchaseOrder")
            .WithSummary("Create a pending purchase order; an empty number gets the next PO- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SavePurchaseOrderRequest>();

        orders.MapPut("/{id:int}", Update)
            .WithName("UpdatePurchaseOrder")
            .WithSummary("Change a pending purchase order")
            .RequirePermission($"{Feature}.edit")
            .Validate<SavePurchaseOrderRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapGet("/{id:int}/approval", Approval)
            .WithName("GetPurchaseOrderApproval")
            .WithSummary("Approval steps of a purchase order and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("/{id:int}/approve", Approve)
            .WithName("ApprovePurchaseOrder")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectPurchaseOrder")
            .WithSummary("Reject a pending purchase order; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{ProcurementMenu.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapPost("/{id:int}/close", Close)
            .WithName("ClosePurchaseOrder")
            .WithSummary("Close an approved purchase order so nothing more is received against it")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapDelete("/{id:int}", Delete)
            .WithName("DeletePurchaseOrder")
            .WithSummary("Delete a pending or rejected purchase order")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<PurchaseOrderList> List(
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
        var orders = db.PurchaseOrders.AsNoTracking().Where(o => o.IsActive);
        if (ProcurementData.Trimmed(search) is { } term)
        {
            orders = orders.Where(o => o.Code.Contains(term) ||
                (o.ReferenceNo != null && o.ReferenceNo.Contains(term)) ||
                (o.Remarks != null && o.Remarks.Contains(term)) ||
                db.Suppliers.Any(s => s.Id == o.SupplierId && s.Name.Contains(term)) ||
                o.Lines.Any(l => l.Item!.Name.Contains(term) || l.Item.Code.Contains(term)));
        }

        if (supplierId is not null) orders = orders.Where(o => o.SupplierId == supplierId);
        if (warehouseId is not null) orders = orders.Where(o => o.WarehouseId == warehouseId);
        if (paymentTypeId is not null) orders = orders.Where(o => o.PaymentTypeId == paymentTypeId);

        var (filtered, counts) = await ProcurementData.ByStatusAsync(orders, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(o => o.Code),
            "-code" => filtered.OrderByDescending(o => o.Code),
            "orderDate" => filtered.OrderBy(o => o.OrderDate).ThenBy(o => o.Id),
            "expectedReceiveDate" => filtered.OrderBy(o => o.ExpectedReceiveDate),
            "-expectedReceiveDate" => filtered.OrderByDescending(o => o.ExpectedReceiveDate),
            _ => filtered.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id),
        };

        var rows = await ProcurementData.Page(sorted, page, pageSize)
            .Select(o => new
            {
                o.Id,
                o.Code,
                o.OrderDate,
                o.SupplierId,
                SupplierName = db.Suppliers.Where(s => s.Id == o.SupplierId).Select(s => s.Name).FirstOrDefault(),
                o.WarehouseId,
                WarehouseName = db.Warehouses.Where(w => w.Id == o.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                o.ExpectedReceiveDate,
                RequisitionCode = db.PurchaseRequisitions.Where(r => r.Id == o.RequisitionId).Select(r => r.Code).FirstOrDefault(),
                o.Status,
                ItemCount = o.Lines.Count,
                o.TotalQuantity,
                o.NetAmount,
                o.CreatedAt,
                o.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new PurchaseOrderList(
            rows.Select(o => new PurchaseOrderRow(
                o.Id, o.Code, o.OrderDate, o.SupplierId, o.SupplierName, o.WarehouseId, o.WarehouseName, o.ExpectedReceiveDate,
                o.RequisitionCode, ApprovalFlow.StatusKey(o.Status), o.ItemCount, o.TotalQuantity, o.NetAmount, o.CreatedAt,
                o.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<List<OrderRequisitionOption>> Requisitions(ErpDbContext db, CancellationToken cancellationToken)
    {
        var requisitions = await db.PurchaseRequisitions.AsNoTracking()
            .Where(r => r.IsActive && r.Status == ApprovalStatus.Approved)
            .OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.RequestDate,
                r.WarehouseId,
                Lines = r.Rows.Where(l => l.Quantity > l.OrderedQuantity).OrderBy(l => l.Sequence).Select(l => new OrderRequisitionLine(
                    l.ItemId, l.Item!.Code, l.Item.Name, l.Uom!.Name, l.Quantity - l.OrderedQuantity, l.Rate,
                    l.Item.PurchaseVatPercentage ?? 0)).ToList(),
            })
            .ToListAsync(cancellationToken);

        return [.. requisitions
            .Where(r => r.Lines.Count > 0)
            .Select(r => new OrderRequisitionOption(r.Id, r.Code, r.Code, r.RequestDate, r.WarehouseId, true, r.Lines))];
    }

    private static async Task<Results<Ok<PurchaseOrderDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<PurchaseOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SavePurchaseOrderRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var order = new PurchaseOrder
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(order, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<PurchaseOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SavePurchaseOrderRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        if (order is null) return TypedResults.NotFound();
        if (order.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(order, request, current, db, configs, cancellationToken);
    }

    private static Task<Results<Ok<PurchaseOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, o => ApprovalFlow.ActAsync(db, current, Feature, o, true, request.Comment, cancellationToken), cancellationToken);

    private static async Task<Results<Ok<PurchaseOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, o => ApprovalFlow.ActAsync(db, current, Feature, o, false, request.Comment, cancellationToken),
                cancellationToken);

    private static Task<Results<Ok<PurchaseOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Close(
        int id, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, o => Task.FromResult(ProcurementData.Close(o)), cancellationToken);

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        return order is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, order, cancellationToken));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        if (order is null) return TypedResults.NotFound();
        if (ProcurementData.Delete(order) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await RequisitionOrders.SyncAsync(db, [order.RequisitionId], cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<PurchaseOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<PurchaseOrder, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        if (order is null) return TypedResults.NotFound();
        if (await change(order) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        await RequisitionOrders.SyncAsync(db, [order.RequisitionId], cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<PurchaseOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        PurchaseOrder order, SavePurchaseOrderRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = order.Id > 0 ? ToRequest(order) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await ProcurementData.CodeAsync(db.PurchaseOrders, order, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("paymentTypeId", request.PaymentTypeId, existing?.PaymentTypeId, ProcurementLookups.PaymentType),
            ("itemTypeId", request.ItemTypeId, existing?.ItemTypeId, ItemLookups.ItemType),
        ], errors, cancellationToken);
        await ProcurementData.CheckWarehouseAsync(db, request.WarehouseId, existing?.WarehouseId, errors, cancellationToken);
        await CheckSourcesAsync(db, request, existing, errors, cancellationToken);
        var items = await ProcurementData.CheckItemsAsync(
            db, [.. request.Lines.Select(l => l.ItemId)], order.Lines.Select(l => l.ItemId), errors, cancellationToken);

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var lines = request.Lines.Select((line, index) => Priced(new PurchaseOrderLine
        {
            Sequence = index + 1,
            ItemId = line.ItemId,
            UomId = items[line.ItemId].UomId,
            Quantity = line.Quantity,
            Price = line.Price ?? items[line.ItemId].Rate,
            Discount = line.Discount ?? 0,
            VatPercentage = line.VatPercentage ?? items[line.ItemId].VatPercentage,
            Specification = ProcurementData.Trimmed(line.Specification),
        }, request.IsVatInclusive)).ToList();
        var itemTotal = lines.Sum(l => l.NetAmount);
        var headerDiscount = request.HeaderDiscount ?? 0;

        if (lines.Any(l => l.Discount > l.TotalAmount)) errors["lines"] = ["A discount cannot be more than the line amount."];
        else if (headerDiscount > itemTotal) errors["headerDiscount"] = ["The discount cannot be more than the item total."];
        if (request.RequisitionId is { } requisitionId)
        {
            var remaining = await RequisitionOrders.RemainingAsync(db, requisitionId, order.Id, cancellationToken);
            var over = lines
                .Where(l => remaining.TryGetValue(l.ItemId, out var left) && l.Quantity > left)
                .Select(l => $"Only {remaining[l.ItemId]:0.####} of {items[l.ItemId].Code} is left on the requisition.")
                .ToList();
            if (over.Count > 0) errors["lines"] = [.. over];
        }

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var previousRequisition = order.RequisitionId;
        order.Lines.Clear();
        order.Lines.AddRange(lines);
        order.HeaderDiscount = headerDiscount;
        order.DeliveryCharge = request.DeliveryCharge ?? 0;
        order.LaborCharge = request.LaborCharge ?? 0;
        order.OtherCharge = request.OtherCharge ?? 0;
        order.TotalQuantity = lines.Sum(l => l.Quantity);
        order.TotalVatAmount = lines.Sum(l => l.VatAmount);
        order.ItemTotalAmount = itemTotal;
        order.NetAmount = itemTotal - order.HeaderDiscount + order.DeliveryCharge + order.LaborCharge + order.OtherCharge;

        if (order.Id == 0) db.PurchaseOrders.Add(order);
        order.Code = code ?? await ProcurementData.NextCodeAsync(db.PurchaseOrders, CodePrefix, cancellationToken);
        order.OrderDate = request.OrderDate;
        order.SupplierId = request.SupplierId;
        order.WarehouseId = request.WarehouseId;
        order.RequisitionId = request.RequisitionId;
        order.ReferenceNo = ProcurementData.Trimmed(request.ReferenceNo);
        order.ExpectedReceiveDate = request.ExpectedReceiveDate;
        order.PaymentTypeId = request.PaymentTypeId;
        order.PaymentDays = request.PaymentDays;
        order.IsVatInclusive = request.IsVatInclusive;
        order.ItemTypeId = request.ItemTypeId;
        order.ShippingAddress = ProcurementData.Trimmed(request.ShippingAddress);
        order.Remarks = ProcurementData.Trimmed(request.Remarks);
        order.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, order.Id, cancellationToken);
        await RequisitionOrders.SyncAsync(db, [previousRequisition, order.RequisitionId], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, order.Id, cancellationToken))!);
    }

    private static PurchaseOrderLine Priced(PurchaseOrderLine line, bool vatInclusive)
    {
        line.TotalAmount = Math.Round(line.Quantity * line.Price, 2);
        var taxable = line.TotalAmount - line.Discount;
        line.VatAmount = Math.Round(vatInclusive
            ? taxable * line.VatPercentage / (100 + line.VatPercentage)
            : taxable * line.VatPercentage / 100, 2);
        line.NetAmount = vatInclusive ? taxable : taxable + line.VatAmount;
        return line;
    }

    private static async Task CheckSourcesAsync(
        ErpDbContext db, SavePurchaseOrderRequest request, SavePurchaseOrderRequest? existing, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        if (request.SupplierId != existing?.SupplierId &&
            !await db.Suppliers.AnyAsync(s => s.Id == request.SupplierId && s.IsActive, cancellationToken))
        {
            errors["supplierId"] = ["Choose an active supplier of this business unit."];
        }

        if (request.RequisitionId is { } requisitionId && requisitionId != existing?.RequisitionId &&
            !await db.PurchaseRequisitions.AnyAsync(r => r.Id == requisitionId && r.IsActive && r.Status == ApprovalStatus.Approved,
                cancellationToken))
        {
            errors["requisitionId"] = ["Choose an approved requisition of this business unit."];
        }
    }

    private static SavePurchaseOrderRequest ToRequest(PurchaseOrder o) => new()
    {
        Code = o.Code,
        OrderDate = o.OrderDate,
        SupplierId = o.SupplierId,
        WarehouseId = o.WarehouseId,
        RequisitionId = o.RequisitionId,
        ReferenceNo = o.ReferenceNo,
        ExpectedReceiveDate = o.ExpectedReceiveDate,
        PaymentTypeId = o.PaymentTypeId,
        PaymentDays = o.PaymentDays,
        IsVatInclusive = o.IsVatInclusive,
        ItemTypeId = o.ItemTypeId,
        ShippingAddress = o.ShippingAddress,
        HeaderDiscount = o.HeaderDiscount,
        DeliveryCharge = o.DeliveryCharge,
        LaborCharge = o.LaborCharge,
        OtherCharge = o.OtherCharge,
        Remarks = o.Remarks,
        Lines = [.. o.Lines.OrderBy(l => l.Sequence).Select(l => new SavePurchaseOrderLine
        {
            ItemId = l.ItemId,
            Quantity = l.Quantity,
            Price = l.Price,
            Discount = l.Discount,
            VatPercentage = l.VatPercentage,
            Specification = l.Specification,
        })],
        CustomData = CustomFieldData.Parse(o.CustomData),
    };

    private static async Task<PurchaseOrderDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var o = await db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .Include(x => x.Lines).ThenInclude(l => l.Uom)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (o is null) return null;

        var names = await PartnerData.LookupNamesAsync(db, [o.PaymentTypeId, o.ItemTypeId], cancellationToken);
        string? Lookup(int? key) => key is { } k ? names.GetValueOrDefault(k) : null;

        return new PurchaseOrderDetail
        {
            Id = o.Id,
            Code = o.Code,
            OrderDate = o.OrderDate,
            SupplierId = o.SupplierId,
            SupplierName = await db.Suppliers.Where(s => s.Id == o.SupplierId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken),
            WarehouseId = o.WarehouseId,
            WarehouseName = await db.Warehouses.Where(w => w.Id == o.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            RequisitionId = o.RequisitionId,
            RequisitionName = await db.PurchaseRequisitions.Where(r => r.Id == o.RequisitionId).Select(r => r.Code)
                .FirstOrDefaultAsync(cancellationToken),
            ReferenceNo = o.ReferenceNo,
            ExpectedReceiveDate = o.ExpectedReceiveDate,
            PaymentTypeId = o.PaymentTypeId,
            PaymentTypeName = Lookup(o.PaymentTypeId),
            PaymentDays = o.PaymentDays,
            IsVatInclusive = o.IsVatInclusive,
            ItemTypeId = o.ItemTypeId,
            ItemTypeName = Lookup(o.ItemTypeId),
            ShippingAddress = o.ShippingAddress,
            HeaderDiscount = o.HeaderDiscount,
            DeliveryCharge = o.DeliveryCharge,
            LaborCharge = o.LaborCharge,
            OtherCharge = o.OtherCharge,
            Remarks = o.Remarks,
            Status = ApprovalFlow.StatusKey(o.Status),
            ReviewedAt = o.ReviewedAt,
            ReviewedByName = await ProcurementData.ReviewerNameAsync(db, o.ReviewedById, cancellationToken),
            ReviewComment = o.ReviewComment,
            TotalQuantity = o.TotalQuantity,
            ReceivedQuantity = o.ReceivedQuantity,
            ItemTotalAmount = o.ItemTotalAmount,
            TotalVatAmount = o.TotalVatAmount,
            NetAmount = o.NetAmount,
            Lines = [.. o.Lines.OrderBy(l => l.Sequence).Select(l => new PurchaseOrderLineDetail(
                l.Id, l.ItemId, l.Item!.Code, l.Item.Name, l.UomId, l.Uom!.Name, l.Quantity, l.ReceivedQuantity, l.Price,
                l.TotalAmount, l.Discount, l.VatPercentage, l.VatAmount, l.NetAmount, l.Specification))],
            CustomData = CustomFieldData.Parse(o.CustomData),
            CreatedAt = o.CreatedAt,
            ModifiedAt = o.ModifiedAt,
        };
    }
}
