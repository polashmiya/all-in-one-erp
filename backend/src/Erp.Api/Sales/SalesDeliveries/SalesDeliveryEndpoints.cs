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

namespace Erp.Api.Sales.SalesDeliveries;

internal static class SalesDeliveryEndpoints
{
    private const string Feature = SalesMenu.SalesDelivery;
    private const string CodePrefix = "SD-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static string TypeKey(DeliveryType type) => type.ToString().ToLowerInvariant();

    public static DeliveryType? ParseType(string? type) =>
        Enum.TryParse<DeliveryType>(type, true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    public static void Map(IEndpointRouteBuilder group)
    {
        var deliveries = group.MapGroup("/sales-deliveries");

        deliveries.MapGet("", List)
            .WithName("ListSalesDeliveries")
            .WithSummary("Sales deliveries of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        deliveries.MapGet("/orders", Orders)
            .WithName("GetDeliveryOrderOptions")
            .WithSummary("Approved sales orders with what is left to deliver")
            .RequireAnyPermission(Editors);

        deliveries.MapGet("/{id:int}", Get)
            .WithName("GetSalesDelivery")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        deliveries.MapPost("", Create)
            .WithName("CreateSalesDelivery")
            .WithSummary("Create a pending delivery against a sales order or directly; an empty number gets the next SD- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveSalesDeliveryRequest>();

        deliveries.MapPut("/{id:int}", Update)
            .WithName("UpdateSalesDelivery")
            .WithSummary("Change a pending sales delivery")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveSalesDeliveryRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deliveries.MapPost("/{id:int}/confirm", Confirm)
            .WithName("ConfirmSalesDelivery")
            .WithSummary("Record that the customer received an approved delivery")
            .RequirePermission($"{Feature}.edit")
            .Validate<ConfirmDeliveryRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deliveries.MapGet("/{id:int}/approval", Approval)
            .WithName("GetSalesDeliveryApproval")
            .WithSummary("Approval steps of a sales delivery and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        deliveries.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveSalesDelivery")
            .WithSummary("Approve a delivery; the last approval takes the stock out and posts the sale")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deliveries.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectSalesDelivery")
            .WithSummary("Reject a pending delivery; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deliveries.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelSalesDelivery")
            .WithSummary("Cancel an approved delivery the customer has not received; stock and accounts are reversed")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deliveries.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSalesDelivery")
            .WithSummary("Delete a pending or rejected sales delivery")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<SalesDeliveryList> List(
        string? search,
        string? status,
        string? type,
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
        var deliveries = db.SalesDeliveries.AsNoTracking().Where(d => d.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            deliveries = deliveries.Where(d => d.Code.Contains(term) || d.CustomerName.Contains(term) ||
                (d.OrderCode != null && d.OrderCode.Contains(term)) ||
                (d.ChallanNo != null && d.ChallanNo.Contains(term)) ||
                (d.CustomerPo != null && d.CustomerPo.Contains(term)) ||
                d.Lines.Any(l => l.ItemName.Contains(term) || l.Item!.Code.Contains(term)));
        }

        if (ParseType(type) is { } only) deliveries = deliveries.Where(d => d.Type == only);
        if (customerId is not null) deliveries = deliveries.Where(d => d.CustomerId == customerId);
        if (warehouseId is not null) deliveries = deliveries.Where(d => d.WarehouseId == warehouseId);
        if (salesForceId is not null) deliveries = deliveries.Where(d => d.SalesForceId == salesForceId);
        if (paymentTypeId is not null) deliveries = deliveries.Where(d => d.PaymentTypeId == paymentTypeId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(deliveries, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(d => d.Code),
            "-code" => filtered.OrderByDescending(d => d.Code),
            "deliveryDate" => filtered.OrderBy(d => d.DeliveryDate).ThenBy(d => d.Id),
            _ => filtered.OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.Type,
                d.DeliveryDate,
                d.CustomerId,
                d.CustomerName,
                d.OrderId,
                d.OrderCode,
                d.WarehouseId,
                WarehouseName = db.Warehouses.Where(w => w.Id == d.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                d.ChallanNo,
                d.SalesForceId,
                d.SalesForceName,
                d.Status,
                d.IsConfirmed,
                ItemCount = d.Lines.Count,
                d.TotalQuantity,
                d.NetAmount,
                d.PendingAmount,
                d.CreatedAt,
                d.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new SalesDeliveryList(
            rows.Select(d => new SalesDeliveryRow(
                d.Id, d.Code, TypeKey(d.Type), d.DeliveryDate, d.CustomerId, d.CustomerName, d.OrderId, d.OrderCode, d.WarehouseId,
                d.WarehouseName, d.ChallanNo, d.SalesForceId, d.SalesForceName, ApprovalFlow.StatusKey(d.Status), d.IsConfirmed,
                d.ItemCount, d.TotalQuantity, d.NetAmount, d.PendingAmount, d.CreatedAt, d.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<List<DeliveryOrderOption>> Orders(ErpDbContext db, CancellationToken cancellationToken)
    {
        var orders = await db.SalesOrders.AsNoTracking()
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Where(o => o.IsActive && o.Status == ApprovalStatus.Approved && !o.IsCompleted)
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
        var ids = orders.Select(o => o.Id).ToList();
        var phones = await db.Customers.AsNoTracking()
            .Where(c => db.SalesOrders.Any(o => ids.Contains(o.Id) && o.CustomerId == c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.ContactMobile ?? c.Mobile, cancellationToken);
        var charged = await db.SalesDeliveries.AsNoTracking()
            .Where(d => d.IsActive && d.OrderId != null && ids.Contains(d.OrderId.Value) &&
                d.Status != ApprovalStatus.Rejected && d.Status != ApprovalStatus.Cancelled)
            .GroupBy(d => d.OrderId!.Value)
            .Select(g => new
            {
                g.Key,
                OthersCost = g.Sum(d => d.OthersCost),
                ExtraCharge = g.Sum(d => d.ExtraCharge),
                Deduction = g.Sum(d => d.DeductionValue),
                Special = g.Sum(d => d.SpecialDiscountAmount),
            })
            .ToDictionaryAsync(g => g.Key, cancellationToken);

        return [.. orders
            .Select(o =>
            {
                var used = charged.GetValueOrDefault(o.Id);
                return new DeliveryOrderOption
                {
                    Id = o.Id,
                    Code = o.Code,
                    Name = o.Code,
                    IsActive = true,
                    OrderDate = o.OrderDate,
                    CustomerId = o.CustomerId,
                    CustomerName = o.CustomerName,
                    Phone = phones.GetValueOrDefault(o.CustomerId),
                    OfficeId = o.OfficeId,
                    WarehouseId = o.WarehouseId,
                    SalesForceId = o.SalesForceId,
                    PriceTypeId = o.PriceTypeId,
                    ProjectName = o.ProjectName,
                    ReferredByPartnerId = o.ReferredByPartnerId,
                    CustomerPo = o.CustomerPo,
                    CustomerPoDate = o.CustomerPoDate,
                    PaymentTypeId = o.PaymentTypeId,
                    PaymentTermDays = o.PaymentTermDays,
                    ShippingContactPerson = o.ShippingContactPerson,
                    ShippingAddressId = o.ShippingAddressId,
                    ShippingAddressName = o.ShippingAddressName,
                    IsVatInclusive = o.IsVatInclusive,
                    SpecialDiscountAmount = Math.Max(0, o.SpecialDiscountAmount - (used?.Special ?? 0)),
                    OthersCost = Math.Max(0, o.OthersCost - (used?.OthersCost ?? 0)),
                    OtherChargeAccountId = o.OtherChargeAccountId,
                    ExtraCharge = Math.Max(0, o.ExtraCharge - (used?.ExtraCharge ?? 0)),
                    ExtraChargeAccountId = o.ExtraChargeAccountId,
                    DeductionLabel = o.DeductionLabel,
                    DeductionValue = Math.Max(0, o.DeductionValue - (used?.Deduction ?? 0)),
                    FreeTypeId = o.FreeTypeId,
                    CurrencyId = o.CurrencyId,
                    ExchangeRate = o.ExchangeRate,
                    IsAdvanceAdjust = o.IsAdvanceAdjust,
                    AdvanceAvailable = o.AdvanceRemainingAmount,
                    Lines =
                    [
                        .. o.Lines.OrderBy(l => l.Sequence)
                            .Where(l => l.Quantity > l.DeliveredQuantity + l.ScheduledQuantity)
                            .Select(l => new DeliveryOrderLine(
                                l.Id, l.ItemId, l.Item!.Code, l.ItemName, l.UomName, l.ItemDescription, l.Quantity,
                                l.Quantity - l.DeliveredQuantity - l.ScheduledQuantity, l.Price,
                                Math.Round(l.DiscountAmount / l.Quantity, 6), l.SdPercentage, l.VatPercentage, l.AitPercentage,
                                l.WarrantyInMonth, l.ShelfLife, l.IsOfferItem)),
                    ],
                };
            })
            .Where(o => o.Lines.Count > 0)];
    }

    private static async Task<Results<Ok<SalesDeliveryDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveSalesDeliveryRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var delivery = new SalesDelivery
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(delivery, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveSalesDeliveryRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var delivery = await FindAsync(db, id, cancellationToken);
        if (delivery is null) return TypedResults.NotFound();
        if (delivery.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(delivery, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Confirm(
        int id, ConfirmDeliveryRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        await SalesData.CheckEmployeeAsync(db, "collectedById", request.CollectedById, null, errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        return await ChangeAsync(id, db, async d =>
        {
            if (d.Status != ApprovalStatus.Approved || d.IsConfirmed)
            {
                return TypedResults.Problem(
                    detail: "Only an approved delivery that is not confirmed yet can be confirmed.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            d.IsConfirmed = true;
            d.IsComplete = true;
            d.ConfirmedById = current.UserId;
            d.ConfirmedByName = await DocumentData.ReviewerNameAsync(db, current.UserId, cancellationToken);
            d.ConfirmedDate = DateTime.UtcNow;
            d.ActualDeliveryDate = request.ActualDeliveryDate;
            d.CollectedById = request.CollectedById;
            d.CollectedByName = await SalesData.EmployeeNameAsync(db, request.CollectedById, cancellationToken);
            d.GrnNo = DocumentData.Trimmed(request.GrnNo);
            d.GrnDate = request.GrnDate;
            d.GrnAmount = request.GrnAmount;
            d.GrnCollectionDate = request.GrnCollectionDate;
            d.ConfirmRemarks = DocumentData.Trimmed(request.Remarks);
            return null;
        }, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var delivery = await db.SalesDeliveries.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        return delivery is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, delivery, cancellationToken));
    }

    private static Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async d =>
            await ApprovalFlow.ActAsync(db, current, Feature, d, true, request.Comment, cancellationToken) ??
            (d.Status == ApprovalStatus.Approved ? await SalesPosting.DeliveryAsync(db, current, d, cancellationToken) : null),
            cancellationToken);

    private static async Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, d => ApprovalFlow.ActAsync(db, current, Feature, d, false, request.Comment, cancellationToken),
                cancellationToken);

    private static async Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db,
                d => ApprovalFlow.CancelAsync(db, current, Feature, d, request.Comment, async () => d.IsConfirmed
                    ? ApprovalFlow.InUse("The customer has already received it. Use a sales return.")
                    : await StockPosting.CancelAsync(db, current, Feature, d.Id, d.DeliveryDate, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var delivery = await db.SalesDeliveries.SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (delivery is null) return TypedResults.NotFound();
        if (DocumentData.Delete(delivery) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await OrderDeliveries.SyncAsync(db, [delivery.OrderId], cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<SalesDelivery?> FindAsync(ErpDbContext db, int id, CancellationToken cancellationToken) =>
        db.SalesDeliveries.Include(d => d.Lines).SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);

    private static async Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<SalesDelivery, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var delivery = await FindAsync(db, id, cancellationToken);
        if (delivery is null) return TypedResults.NotFound();
        if (await change(delivery) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        await OrderDeliveries.SyncAsync(db, [delivery.OrderId], cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<SalesDeliveryDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        SalesDelivery delivery, SaveSalesDeliveryRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = delivery.Id > 0 ? new SaveSalesDeliveryRequest(delivery) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        SalesOrder? order = null;
        if (request.OrderId is { } orderId)
        {
            var currentOrder = existing?.OrderId;
            order = await db.SalesOrders.AsNoTracking()
                .Include(o => o.Lines).ThenInclude(l => l.Item)
                .SingleOrDefaultAsync(o => o.Id == orderId && o.IsActive &&
                    (o.Status == ApprovalStatus.Approved || o.Id == currentOrder), cancellationToken);
            if (order is null)
            {
                errors["orderId"] = ["Choose an approved sales order of this business unit."];
                return TypedResults.ValidationProblem(errors);
            }

            request = request with
            {
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                IsVatInclusive = order.IsVatInclusive,
                IsGrossDiscount = false,
                Lines = await FromOrderAsync(db, order, delivery.Id, request.Lines, errors, cancellationToken),
            };
        }

        var code = await DocumentData.CodeAsync(db.SalesDeliveries, delivery, request.Code, errors, cancellationToken);
        await PeriodLock.CheckAsync(db, "deliveryDate", request.DeliveryDate, errors, cancellationToken);
        await SalesData.CheckAsync(
            db, request, order is null ? existing : new SaveSalesDeliveryRequest(delivery) { CustomerId = order.CustomerId }, errors,
            cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("deliveryMethodId", request.DeliveryMethodId, existing?.DeliveryMethodId, SalesLookups.DeliveryMethod),
            ("installmentTypeId", request.InstallmentTypeId, existing?.InstallmentTypeId, SalesLookups.InstallmentType),
        ], errors, cancellationToken);
        await SalesData.CheckEmployeeAsync(
            db, "courierOrEmployeeId", request.CourierOrEmployeeId, existing?.CourierOrEmployeeId, errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var items = await SalesData.CheckItemsAsync(
            db, request.Lines, [.. delivery.Lines.Select(l => l.ItemId), .. order?.Lines.Select(l => l.ItemId) ?? []], errors,
            cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var itemIds = items.Keys.ToList();
        StockData.CheckEnough(
            request.Lines.GroupBy(l => l.ItemId).Select(g => (g.Key, g.Sum(l => l.Quantity))), items,
            await StockData.OnHandAsync(db, request.WarehouseId, itemIds, cancellationToken), errors);

        var lines = request.Lines.Select((line, index) => SalesData.Line(new SalesDeliveryLine
        {
            OrderId = order?.Id,
            OrderCode = order?.Code,
            OrderLineId = line.OrderLineId,
            WarehouseId = request.WarehouseId,
            Batch = DocumentData.Trimmed(line.Batch),
            ItemSerial = DocumentData.Trimmed(line.ItemSerial),
            PackQuantity = DocumentData.Trimmed(line.PackQuantity),
            WarrantyDescription = DocumentData.Trimmed(line.WarrantyDescription),
            WarrantyExpiredDate = line.WarrantyExpiredDate ??
                (line.WarrantyInMonth > 0 ? request.DeliveryDate.AddMonths(line.WarrantyInMonth.Value) : null),
        }, index, line, items[line.ItemId])).ToList();
        SalesPricing.Price(
            lines,
            new DiscountRule(request.IsVatInclusive, request.IsGrossDiscount, request.HeaderDiscountPercentage ?? 0,
                request.HeaderDiscount ?? 0, "headerDiscount"),
            errors);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        SalesData.Total(delivery, lines, request, errors);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var adjust = request.AdvanceBalanceAdjust ?? 0;
        var received = request.ReceiveAmount ?? 0;
        var available = order is null ? 0 : await OrderDeliveries.AdvanceAvailableAsync(db, order, delivery.Id, cancellationToken);
        if (adjust > available) errors["advanceBalanceAdjust"] = [$"Only {available:0.##} of the order's advance is left."];
        else if (adjust > delivery.NetAmount) errors["advanceBalanceAdjust"] = ["This is more than the net amount."];
        else if (received > delivery.NetAmount - adjust) errors["receiveAmount"] = ["This is more than what the customer has to pay."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var previousOrder = delivery.OrderId;
        delivery.Lines.Clear();
        delivery.Lines.AddRange(lines);
        if (delivery.Id == 0) db.SalesDeliveries.Add(delivery);
        await SalesData.ApplyAsync(db, delivery, request, cancellationToken);
        var names = await PartnerData.LookupNamesAsync(db, [request.DeliveryMethodId, request.InstallmentTypeId], cancellationToken);
        string? Name(int? id) => id is { } key ? names.GetValueOrDefault(key) : null;

        delivery.Code = code ?? await DocumentData.NextCodeAsync(db.SalesDeliveries, CodePrefix, cancellationToken);
        delivery.Type = order is null ? DeliveryType.Direct : DeliveryType.Order;
        delivery.OrderId = order?.Id;
        delivery.OrderCode = order?.Code;
        delivery.Phone = DocumentData.Trimmed(request.Phone);
        delivery.ChallanNo = DocumentData.Trimmed(request.ChallanNo);
        delivery.VatChallanNo = DocumentData.Trimmed(request.VatChallanNo);
        delivery.BillNo = DocumentData.Trimmed(request.BillNo);
        delivery.RefInvoice = DocumentData.Trimmed(request.RefInvoice);
        delivery.AnonymousAddress = DocumentData.Trimmed(request.AnonymousAddress);
        delivery.Narration = DocumentData.Trimmed(request.Narration);
        delivery.DeliveryMethodId = request.DeliveryMethodId;
        delivery.DeliveryMethodName = Name(request.DeliveryMethodId);
        delivery.CourierOrEmployeeId = request.CourierOrEmployeeId;
        delivery.CourierOrEmployeeName = await SalesData.EmployeeNameAsync(db, request.CourierOrEmployeeId, cancellationToken);
        delivery.SubmissionDate = request.SubmissionDate;
        delivery.AlternativeDeliveryDate = request.AlternativeDeliveryDate;
        delivery.AdvanceBalanceAdjust = adjust;
        delivery.CustomerNetAmount = delivery.NetAmount - adjust;
        delivery.ReceiveAmount = received;
        delivery.PendingAmount = delivery.CustomerNetAmount - received;
        delivery.InterestRate = request.InterestRate ?? 0;
        delivery.NetAmountWithInterest = SalesPricing.Round(delivery.PendingAmount * (1 + delivery.InterestRate / 100));
        delivery.TotalNoOfInstallment = request.TotalNoOfInstallment ?? 0;
        delivery.InstallmentStartDate = request.InstallmentStartDate;
        delivery.InstallmentTypeId = request.InstallmentTypeId;
        delivery.InstallmentTypeName = Name(request.InstallmentTypeId);
        delivery.AmountPerInstallment = delivery.TotalNoOfInstallment == 0
            ? 0
            : SalesPricing.Round(delivery.NetAmountWithInterest / delivery.TotalNoOfInstallment);
        delivery.CommissionPercentage = request.CommissionPercentage ?? 0;
        delivery.IsExportSales = request.IsExportSales;

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, delivery.Id, cancellationToken);
        await OrderDeliveries.SyncAsync(db, [previousOrder, delivery.OrderId], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, delivery.Id, cancellationToken))!);
    }

    private static async Task<List<SaveSalesDeliveryLine>> FromOrderAsync(
        ErpDbContext db, SalesOrder order, int deliveryId, IReadOnlyList<SaveSalesDeliveryLine> sent, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var used = await OrderDeliveries.UsedAsync(db, [order.Id], deliveryId, cancellationToken);
        var lines = new List<SaveSalesDeliveryLine>();
        var problems = new List<string>();
        foreach (var line in sent)
        {
            if (order.Lines.Find(l => l.Id == line.OrderLineId) is not { } ordered)
            {
                problems.Add("Every item must be on the sales order.");
                continue;
            }

            var before = used.GetValueOrDefault(ordered.Id);
            var left = ordered.Quantity - before.Quantity;
            if (line.Quantity > left) problems.Add($"Only {Math.Max(0, left):0.####} of {ordered.Item!.Code} is left to deliver.");
            lines.Add(line with
            {
                ItemId = ordered.ItemId,
                Price = ordered.Price,
                IsOfferItem = ordered.IsOfferItem,
                IsAmountWiseDiscount = true,
                IsDiscountPerQuantity = false,
                DiscountPercentage = null,
                DiscountAmount = OrderDeliveries.Discount(ordered, line.Quantity, before),
                SdPercentage = ordered.SdPercentage,
                VatPercentage = ordered.VatPercentage,
                AitPercentage = ordered.AitPercentage,
            });
        }

        if (problems.Count > 0) errors["lines"] = [.. problems.Distinct()];
        return lines;
    }

    private static async Task<SalesDeliveryDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var delivery = await db.SalesDeliveries.AsNoTracking()
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (delivery is null) return null;

        var order = await db.SalesOrders.AsNoTracking().Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == delivery.OrderId, cancellationToken);
        var remaining = new Dictionary<int, decimal>();
        var available = 0m;
        if (order is not null)
        {
            var used = await OrderDeliveries.UsedAsync(db, [order.Id], id, cancellationToken);
            remaining = order.Lines.ToDictionary(l => l.Id, l => l.Quantity - used.GetValueOrDefault(l.Id).Quantity);
            available = await OrderDeliveries.AdvanceAvailableAsync(db, order, id, cancellationToken);
        }

        return new SalesDeliveryDetail(delivery, await SalesData.NamesAsync(db, delivery, cancellationToken), available, remaining);
    }
}
