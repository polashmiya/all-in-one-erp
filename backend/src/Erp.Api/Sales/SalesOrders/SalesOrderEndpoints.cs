using Erp.Api.Accounting;
using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.Inventory;
using Erp.Api.PartnerManagement;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesOrders;

internal static class SalesOrderEndpoints
{
    private const string Feature = SalesMenu.SalesOrder;
    private const string CodePrefix = "SO-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var orders = group.MapGroup("/sales-orders");

        orders.MapGet("", List)
            .WithName("ListSalesOrders")
            .WithSummary("Sales orders of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        orders.MapGet("/quotations", Quotations)
            .WithName("GetOrderQuotationOptions")
            .WithSummary("Approved quotations that are still valid and not ordered yet, with their items")
            .RequireAnyPermission(Editors);

        orders.MapGet("/{id:int}", Get)
            .WithName("GetSalesOrder")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("", Create)
            .WithName("CreateSalesOrder")
            .WithSummary("Create a pending sales order; an empty number gets the next SO- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveSalesOrderRequest>();

        orders.MapPut("/{id:int}", Update)
            .WithName("UpdateSalesOrder")
            .WithSummary("Change a pending sales order")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveSalesOrderRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapGet("/{id:int}/approval", Approval)
            .WithName("GetSalesOrderApproval")
            .WithSummary("Approval steps of a sales order and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveSalesOrder")
            .WithSummary("Approve a sales order; the last approval posts its advance")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectSalesOrder")
            .WithSummary("Reject a pending sales order; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapPost("/{id:int}/close", Close)
            .WithName("CloseSalesOrder")
            .WithSummary("Close an approved sales order so nothing more is delivered against it")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelSalesOrder")
            .WithSummary("Cancel an approved sales order without deliveries; its advance is reversed")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSalesOrder")
            .WithSummary("Delete a pending or rejected sales order")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<SalesOrderList> List(
        string? search,
        string? status,
        int? customerId,
        Guid? warehouseId,
        int? salesForceId,
        int? paymentTypeId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var orders = db.SalesOrders.AsNoTracking().Where(o => o.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            orders = orders.Where(o => o.Code.Contains(term) || o.CustomerName.Contains(term) ||
                (o.SalesQuotationCode != null && o.SalesQuotationCode.Contains(term)) ||
                (o.CustomerPo != null && o.CustomerPo.Contains(term)) ||
                o.Lines.Any(l => l.ItemName.Contains(term) || l.Item!.Code.Contains(term)));
        }

        if (customerId is not null) orders = orders.Where(o => o.CustomerId == customerId);
        if (warehouseId is not null) orders = orders.Where(o => o.WarehouseId == warehouseId);
        if (salesForceId is not null) orders = orders.Where(o => o.SalesForceId == salesForceId);
        if (paymentTypeId is not null) orders = orders.Where(o => o.PaymentTypeId == paymentTypeId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(orders, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(o => o.Code),
            "-code" => filtered.OrderByDescending(o => o.Code),
            "orderDate" => filtered.OrderBy(o => o.OrderDate).ThenBy(o => o.Id),
            "deliveryDate" => filtered.OrderBy(o => o.DeliveryDate),
            "-deliveryDate" => filtered.OrderByDescending(o => o.DeliveryDate),
            _ => filtered.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(o => new
            {
                o.Id,
                o.Code,
                o.OrderDate,
                o.DeliveryDate,
                o.CustomerId,
                o.CustomerName,
                o.SalesQuotationId,
                o.SalesQuotationCode,
                o.WarehouseId,
                WarehouseName = db.Warehouses.Where(w => w.Id == o.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                o.SalesForceId,
                o.SalesForceName,
                o.Status,
                o.IsCompleted,
                ItemCount = o.Lines.Count,
                o.TotalQuantity,
                o.DeliveredQuantity,
                o.NetAmount,
                o.CreatedAt,
                o.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new SalesOrderList(
            rows.Select(o => new SalesOrderRow(
                o.Id, o.Code, o.OrderDate, o.DeliveryDate, o.CustomerId, o.CustomerName, o.SalesQuotationId, o.SalesQuotationCode,
                o.WarehouseId, o.WarehouseName, o.SalesForceId, o.SalesForceName, ApprovalFlow.StatusKey(o.Status), o.IsCompleted,
                o.ItemCount, o.TotalQuantity, o.DeliveredQuantity, o.NetAmount, o.CreatedAt, o.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<List<OrderQuotationOption>> Quotations(ErpDbContext db, CancellationToken cancellationToken)
    {
        var quotations = await QuotationOrders.Open(db).Include(q => q.Lines)
            .OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id)
            .ToListAsync(cancellationToken);
        return [.. quotations.Select(q => new OrderQuotationOption
        {
            Id = q.Id,
            Code = q.Code,
            Name = QuotationName(q),
            IsActive = true,
            CustomerId = q.CustomerId,
            CustomerName = q.CustomerName,
            OfficeId = q.OfficeId,
            SalesForceId = q.SalesForceId,
            ShippingContactPerson = q.ShippingContactPerson,
            ShippingAddressId = q.ShippingAddressId,
            ShippingAddressName = q.ShippingAddressName,
            IsVatInclusive = q.IsVatInclusive,
            IsGrossDiscount = q.IsGrossDiscount,
            HeaderDiscountPercentage = q.DiscountPercentage,
            HeaderDiscount = q.IsGrossDiscount ? q.DiscountAmount : 0,
            OthersCost = q.DeliveryCharge,
            ExtraCharge = q.ExtraCharge,
            Lines = [.. q.Lines.OrderBy(l => l.Sequence).Select(l => new OrderQuotationLine(
                l.ItemId, l.ItemCode, l.ItemName, l.UomName, l.ItemDescription, l.ApproveQuantity, l.ApprovePrice, l.DiscountPercentage,
                l.DiscountAmount, l.IsAmountWiseDiscount, l.IsDiscountPerQuantity, l.SdPercentage, l.VatPercentage, l.AitPercentage,
                l.WarrantyInMonth))],
        })];
    }

    private static string QuotationName(SalesQuotation quotation) => $"{quotation.Code} {quotation.Version}";

    private static async Task<Results<Ok<SalesOrderDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveSalesOrderRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var order = new SalesOrder
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(order, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveSalesOrderRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var order = await FindAsync(db, id, cancellationToken);
        if (order is null) return TypedResults.NotFound();
        if (order.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(order, request, current, db, configs, cancellationToken);
    }

    private static Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async o =>
            await ApprovalFlow.ActAsync(db, current, Feature, o, true, request.Comment, cancellationToken) ??
            (o.Status == ApprovalStatus.Approved ? await SalesPosting.OrderAsync(db, current, o, cancellationToken) : null),
            cancellationToken);

    private static async Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, o => ApprovalFlow.ActAsync(db, current, Feature, o, false, request.Comment, cancellationToken),
                cancellationToken);

    private static Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Close(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async o =>
        {
            if (await SalesCancel.PendingDeliveriesAsync(db, o.Id, cancellationToken) is { } problem) return problem;
            o.ClosedById = current.UserId;
            return DocumentData.Close(o);
        }, cancellationToken);

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        return order is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, order, cancellationToken));
    }

    private static async Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db,
                o => ApprovalFlow.CancelAsync(db, current, Feature, o, request.Comment, async () =>
                    await SalesCancel.OrderAsync(db, o.Id, cancellationToken) ??
                    await StockPosting.CancelAsync(db, current, Feature, o.Id, o.OrderDate, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        if (order is null) return TypedResults.NotFound();
        if (DocumentData.Delete(order) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await QuotationOrders.SyncAsync(db, [order.SalesQuotationId], cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<SalesOrder?> FindAsync(ErpDbContext db, int id, CancellationToken cancellationToken) =>
        db.SalesOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);

    private static async Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<SalesOrder, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var order = await FindAsync(db, id, cancellationToken);
        if (order is null) return TypedResults.NotFound();
        if (await change(order) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        await QuotationOrders.SyncAsync(db, [order.SalesQuotationId], cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<SalesOrderDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        SalesOrder order, SaveSalesOrderRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = order.Id > 0 ? new SaveSalesOrderRequest(order) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await DocumentData.CodeAsync(db.SalesOrders, order, request.Code, errors, cancellationToken);
        await SalesData.CheckAsync(db, request, existing, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("salesTermId", request.SalesTermId, existing?.SalesTermId, SalesLookups.SalesTerm),
            ("lcTypeId", request.LcTypeId, existing?.LcTypeId, SalesLookups.LcType),
            ("paymentTermsId", request.PaymentTermsId, existing?.PaymentTermsId, SalesLookups.PaymentTerms),
        ], errors, cancellationToken);
        var advance = request.AdvanceAmount ?? 0;
        if (advance > 0) await PeriodLock.CheckAsync(db, "orderDate", request.OrderDate, errors, cancellationToken);

        var quotation = await db.SalesQuotations.AsNoTracking().SingleOrDefaultAsync(q => q.Id == request.SalesQuotationId, cancellationToken);
        if (request.SalesQuotationId is { } quotationId && quotationId != existing?.SalesQuotationId &&
            !await QuotationOrders.Open(db).AnyAsync(q => q.Id == quotationId, cancellationToken))
        {
            errors["salesQuotationId"] = ["Choose an approved quotation that is still valid and not ordered yet."];
        }
        else if (quotation is not null && quotation.CustomerId != request.CustomerId)
        {
            errors["customerId"] = ["The quotation is for another customer."];
        }

        var items = await SalesData.CheckItemsAsync(db, request.Lines, order.Lines.Select(l => l.ItemId), errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var lines = request.Lines.Select((line, index) => SalesData.Line(new SalesOrderLine
        {
            DemandQuantity = line.DemandQuantity,
            FobRate = line.FobRate ?? 0,
            FreightRate = line.FreightRate ?? 0,
        }, index, line, items[line.ItemId])).ToList();
        SalesPricing.Price(
            lines,
            new DiscountRule(request.IsVatInclusive, request.IsGrossDiscount, request.HeaderDiscountPercentage ?? 0,
                request.HeaderDiscount ?? 0, "headerDiscount"),
            errors);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
        SalesData.Total(order, lines, request, errors);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
        if (advance > order.NetAmount) errors["advanceAmount"] = ["The advance cannot be more than the net amount."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var previousQuotation = order.SalesQuotationId;
        order.Lines.Clear();
        order.Lines.AddRange(lines);
        if (order.Id == 0) db.SalesOrders.Add(order);
        await SalesData.ApplyAsync(db, order, request, cancellationToken);
        var names = await PartnerData.LookupNamesAsync(db, [request.SalesTermId, request.LcTypeId, request.PaymentTermsId], cancellationToken);
        string? Name(int? id) => id is { } key ? names.GetValueOrDefault(key) : null;

        order.Code = code ?? await DocumentData.NextCodeAsync(db.SalesOrders, CodePrefix, cancellationToken);
        order.SalesQuotationId = request.SalesQuotationId;
        order.SalesQuotationCode = quotation is null ? null : QuotationName(quotation);
        order.AdvanceAmount = advance;
        order.ReceiveAmount = advance;
        order.PendingAmount = order.NetAmount - advance;
        order.DueLimit = request.DueLimit ?? 0;
        order.CustomerPoAmount = request.CustomerPoAmount ?? 0;
        order.CustomerPoRefAmount = request.CustomerPoRefAmount ?? 0;
        order.SalesTermId = request.SalesTermId;
        order.SalesTermName = Name(request.SalesTermId);
        order.DmsInvoice = DocumentData.Trimmed(request.DmsInvoice);
        order.IsForeign = request.IsForeign;
        order.LcTypeId = request.LcTypeId;
        order.LcTypeName = Name(request.LcTypeId);
        order.LcNo = DocumentData.Trimmed(request.LcNo);
        order.PaymentTermsId = request.PaymentTermsId;
        order.PaymentTermsName = Name(request.PaymentTermsId);
        order.IsAllowPartialShipment = request.IsAllowPartialShipment;
        order.IsAllowTransitShipment = request.IsAllowTransitShipment;
        order.Review = DocumentData.Trimmed(request.Review);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, order.Id, cancellationToken);
        await QuotationOrders.SyncAsync(db, [previousQuotation, order.SalesQuotationId], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, order.Id, cancellationToken))!);
    }

    private static async Task<SalesOrderDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.AsNoTracking()
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .SingleOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        if (order is null) return null;

        var deliveries = await db.SalesDeliveries.AsNoTracking()
            .Where(d => d.OrderId == id && d.IsActive)
            .OrderByDescending(d => d.Id)
            .Select(d => new { d.Id, d.Code, d.DeliveryDate, d.TotalQuantity, d.NetAmount, d.Status, d.IsConfirmed })
            .ToListAsync(cancellationToken);
        return new SalesOrderDetail(order, await SalesData.NamesAsync(db, order, cancellationToken),
            [.. deliveries.Select(d => new OrderDelivery(
                d.Id, d.Code, d.DeliveryDate, d.TotalQuantity, d.NetAmount, ApprovalFlow.StatusKey(d.Status), d.IsConfirmed))]);
    }
}
