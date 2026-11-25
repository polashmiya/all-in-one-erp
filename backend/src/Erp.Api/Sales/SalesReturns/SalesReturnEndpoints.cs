using Erp.Api.Accounting;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.PartnerManagement;
using Erp.Api.Procurement;
using Erp.Api.Sales.SalesDeliveries;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesReturns;

internal static class SalesReturnEndpoints
{
    private const string Feature = SalesMenu.SalesReturn;
    private const string CodePrefix = "SR-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var returns = group.MapGroup("/sales-returns");

        returns.MapGet("", List)
            .WithName("ListSalesReturns")
            .WithSummary("Sales returns of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        returns.MapGet("/deliveries", Deliveries)
            .WithName("GetReturnDeliveryOptions")
            .WithSummary("Approved deliveries with what can still come back")
            .RequireAnyPermission(Editors);

        returns.MapGet("/{id:int}", Get)
            .WithName("GetSalesReturn")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        returns.MapPost("", Create)
            .WithName("CreateSalesReturn")
            .WithSummary("Create a pending return against an approved delivery; an empty number gets the next SR- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveSalesReturnRequest>();

        returns.MapPut("/{id:int}", Update)
            .WithName("UpdateSalesReturn")
            .WithSummary("Change a pending sales return")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveSalesReturnRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        returns.MapGet("/{id:int}/approval", Approval)
            .WithName("GetSalesReturnApproval")
            .WithSummary("Approval steps of a sales return and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        returns.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveSalesReturn")
            .WithSummary("Approve a return; the last approval puts the stock back and posts the accounts")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        returns.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectSalesReturn")
            .WithSummary("Reject a pending return; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        returns.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelSalesReturn")
            .WithSummary("Cancel an approved return; stock and accounts are reversed")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        returns.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSalesReturn")
            .WithSummary("Delete a pending or rejected sales return")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<SalesReturnList> List(
        string? search,
        string? status,
        int? customerId,
        Guid? warehouseId,
        int? returnReasonId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var returns = db.SalesReturns.AsNoTracking().Where(r => r.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            returns = returns.Where(r => r.Code.Contains(term) || r.CustomerName.Contains(term) || r.DeliveryCode.Contains(term) ||
                (r.CreditNoteNo != null && r.CreditNoteNo.Contains(term)) ||
                (r.ReturnRefNo != null && r.ReturnRefNo.Contains(term)) ||
                r.Lines.Any(l => l.ItemName.Contains(term) || l.Item!.Code.Contains(term)));
        }

        if (customerId is not null) returns = returns.Where(r => r.CustomerId == customerId);
        if (warehouseId is not null) returns = returns.Where(r => r.WarehouseId == warehouseId);
        if (returnReasonId is not null) returns = returns.Where(r => r.ReturnReasonId == returnReasonId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(returns, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(r => r.Code),
            "-code" => filtered.OrderByDescending(r => r.Code),
            "returnDate" => filtered.OrderBy(r => r.ReturnDate).ThenBy(r => r.Id),
            _ => filtered.OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(r => new
            {
                Return = r,
                WarehouseName = db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                ItemCount = r.Lines.Count,
            })
            .ToListAsync(cancellationToken);

        return new SalesReturnList(
            rows.Select(x => new SalesReturnRow(
                x.Return.Id, x.Return.Code, x.Return.ReturnDate, x.Return.DeliveryId, x.Return.DeliveryCode, x.Return.CustomerId,
                x.Return.CustomerName, x.Return.WarehouseId, x.WarehouseName, x.Return.ReturnReasonId, x.Return.ReturnReasonName,
                ApprovalFlow.StatusKey(x.Return.Status), x.ItemCount, x.Return.TotalQuantity, x.Return.NetAmount, x.Return.RefundAmount,
                x.Return.CreatedAt, x.Return.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<List<ReturnDeliveryOption>> Deliveries(ErpDbContext db, CancellationToken cancellationToken)
    {
        var deliveries = await db.SalesDeliveries.AsNoTracking()
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Where(d => d.IsActive && d.Status == ApprovalStatus.Approved && d.Lines.Any(l => l.Quantity > l.ReturnedQuantity))
            .OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.Id)
            .ToListAsync(cancellationToken);
        var ids = deliveries.Select(d => d.Id).ToList();
        var refunded = await DeliveryDues.LiveReturns(db)
            .Where(r => ids.Contains(r.DeliveryId))
            .GroupBy(r => r.DeliveryId)
            .Select(g => new { g.Key, Amount = g.Sum(r => r.ChargeRefundAmount) })
            .ToDictionaryAsync(g => g.Key, g => g.Amount, cancellationToken);

        return [.. deliveries.Select(d => new ReturnDeliveryOption
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Code,
            IsActive = true,
            DeliveryDate = d.DeliveryDate,
            OrderCode = d.OrderCode,
            CustomerId = d.CustomerId,
            CustomerName = d.CustomerName,
            Phone = d.Phone,
            OfficeId = d.OfficeId,
            WarehouseId = d.WarehouseId,
            SalesForceId = d.SalesForceId,
            ChallanNo = d.ChallanNo,
            PickupAddress = d.ShippingAddressName,
            IsVatInclusive = d.IsVatInclusive,
            Due = d.PendingAmount,
            ReductionRate = DeliveryReturns.ReductionRate(d),
            ChargeRefundable = d.OthersCost + d.ExtraCharge - refunded.GetValueOrDefault(d.Id),
            Lines =
            [
                .. d.Lines.Where(l => l.Quantity > l.ReturnedQuantity).OrderBy(l => l.Sequence).Select(l => new ReturnDeliveryLine(
                    l.Id, l.ItemId, l.Item!.Code, l.ItemName, l.UomName, l.ItemDescription, l.Quantity, l.Quantity - l.ReturnedQuantity,
                    l.Price, Math.Round(l.DiscountAmount / l.Quantity, 6), l.SdPercentage, l.VatPercentage, l.AitPercentage, l.IsOfferItem,
                    l.Batch, l.ItemSerial)),
            ],
        })];
    }

    private static async Task<Results<Ok<SalesReturnDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<SalesReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveSalesReturnRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var salesReturn = new SalesReturn
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(salesReturn, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SalesReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveSalesReturnRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var salesReturn = await FindAsync(db, id, cancellationToken);
        if (salesReturn is null) return TypedResults.NotFound();
        if (salesReturn.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(salesReturn, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var salesReturn = await db.SalesReturns.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        return salesReturn is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, salesReturn, cancellationToken));
    }

    private static Task<Results<Ok<SalesReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async r =>
            await ApprovalFlow.ActAsync(db, current, Feature, r, true, request.Comment, cancellationToken) ??
            (r.Status == ApprovalStatus.Approved ? await SalesPosting.ReturnAsync(db, current, r, cancellationToken) : null),
            cancellationToken);

    private static async Task<Results<Ok<SalesReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, r => ApprovalFlow.ActAsync(db, current, Feature, r, false, request.Comment, cancellationToken),
                cancellationToken);

    private static async Task<Results<Ok<SalesReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db,
                r => ApprovalFlow.CancelAsync(db, current, Feature, r, request.Comment,
                    () => SalesCancel.ReturnAsync(db, current, r, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var salesReturn = await db.SalesReturns.SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        if (salesReturn is null) return TypedResults.NotFound();
        if (DocumentData.Delete(salesReturn) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await DeliveryDues.SyncAsync(db, [salesReturn.DeliveryId], cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<SalesReturn?> FindAsync(ErpDbContext db, int id, CancellationToken cancellationToken) =>
        db.SalesReturns.Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

    private static async Task<Results<Ok<SalesReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<SalesReturn, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var salesReturn = await FindAsync(db, id, cancellationToken);
        if (salesReturn is null) return TypedResults.NotFound();
        if (await change(salesReturn) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        await DeliveryDues.SyncAsync(db, [salesReturn.DeliveryId], cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<SalesReturnDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        SalesReturn salesReturn, SaveSalesReturnRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = salesReturn.Id > 0 ? ToRequest(salesReturn) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var delivery = await db.SalesDeliveries.AsNoTracking()
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .SingleOrDefaultAsync(d => d.Id == request.DeliveryId && d.IsActive && d.Status == ApprovalStatus.Approved, cancellationToken);
        if (delivery is null)
        {
            errors["deliveryId"] = ["Choose an approved delivery of this business unit."];
            return TypedResults.ValidationProblem(errors);
        }

        var code = await DocumentData.CodeAsync(db.SalesReturns, salesReturn, request.Code, errors, cancellationToken);
        await PeriodLock.CheckAsync(db, "returnDate", request.ReturnDate, errors, cancellationToken);
        if (request.ReturnDate < delivery.DeliveryDate) errors["returnDate"] = ["Use a date on or after the delivery date."];
        await PartnerData.CheckLookupsAsync(db,
        [
            ("returnTypeId", request.ReturnTypeId, existing?.ReturnTypeId, SalesLookups.ReturnType),
            ("returnReasonId", request.ReturnReasonId, existing?.ReturnReasonId, SalesLookups.ReturnReason),
            ("deliveryMethodId", request.DeliveryMethodId, existing?.DeliveryMethodId, SalesLookups.DeliveryMethod),
            ("paymentMethodId", request.PaymentMethodId, existing?.PaymentMethodId, ProcurementLookups.PaymentMethod),
        ], errors, cancellationToken);
        await DocumentData.CheckWarehouseAsync(db, request.WarehouseId, existing?.WarehouseId, errors, cancellationToken);
        await PaymentSources.CheckAsync(
            db, request.PaymentMethodId, request.BankAccountId, existing?.BankAccountId, errors, cancellationToken);
        await SalesData.CheckPeopleAsync(
            db, delivery.CustomerId, delivery.CustomerId, request.OfficeId, existing?.OfficeId ?? delivery.OfficeId, request.SalesForceId,
            existing?.SalesForceId ?? delivery.SalesForceId, errors, cancellationToken);
        await SalesData.CheckEmployeeAsync(db, "receivedById", request.ReceivedById, existing?.ReceivedById, errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var returned = await DeliveryReturns.LinesAsync(db, [delivery.Id], salesReturn.Id, cancellationToken);
        var lines = Lines(delivery, returned, request, errors);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        SalesPricing.Price(lines, new DiscountRule(delivery.IsVatInclusive, false, 0, 0, "lines"), errors);
        var value = lines.Sum(l => l.TotalValue);
        var reductions = DeliveryReturns.Reductions(delivery);
        var returnsAll = delivery.Lines.All(d =>
            d.Quantity <= returned.GetValueOrDefault(d.Id, ReturnedLine.None).Quantity +
            lines.Where(l => l.DeliveryLineId == d.Id).Sum(l => l.Quantity));
        var reduced = await DeliveryDues.LiveReturns(db)
            .Where(r => r.Id != salesReturn.Id && r.DeliveryId == delivery.Id)
            .SumAsync(r => r.SpecialDiscountAmount, cancellationToken);
        var special = returnsAll
            ? Math.Max(0, reductions - reduced)
            : SalesPricing.Round(value * DeliveryReturns.ReductionRate(delivery));
        var charge = request.ChargeRefundAmount ?? 0;
        var deduction = request.DeductionValue ?? 0;
        var net = value - special + charge - deduction;
        var due = (await DeliveryDues.DueAsync(db, [delivery.Id], salesReturn.Id, 0, cancellationToken))[delivery.Id];
        var dueAdjusted = Math.Clamp(net, 0, Math.Max(0, due));
        var refund = request.RefundAmount ?? 0;

        if (charge > await DeliveryReturns.ChargeRefundableAsync(db, delivery, salesReturn.Id, cancellationToken))
            errors["chargeRefundAmount"] = ["This is more than the charges of the delivery."];
        else if (net < 0) errors["deductionValue"] = ["The deduction cannot be more than the return amount."];
        else if (refund > net - dueAdjusted)
            errors["refundAmount"] = [$"Only {net - dueAdjusted:0.##} can be refunded. The rest reduces what the customer owes."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var previousDelivery = salesReturn.DeliveryId;
        salesReturn.Lines.Clear();
        salesReturn.Lines.AddRange(lines);
        if (salesReturn.Id == 0) db.SalesReturns.Add(salesReturn);
        var names = await PartnerData.LookupNamesAsync(
            db, [request.ReturnTypeId, request.ReturnReasonId, request.DeliveryMethodId], cancellationToken);
        string? Name(int? id) => id is { } key ? names.GetValueOrDefault(key) : null;

        salesReturn.Code = code ?? await DocumentData.NextCodeAsync(db.SalesReturns, CodePrefix, cancellationToken);
        salesReturn.ReturnDate = request.ReturnDate;
        salesReturn.DeliveryId = delivery.Id;
        salesReturn.DeliveryCode = delivery.Code;
        salesReturn.OrderId = delivery.OrderId;
        salesReturn.OrderCode = delivery.OrderCode;
        salesReturn.CustomerId = delivery.CustomerId;
        salesReturn.CustomerName = delivery.CustomerName;
        salesReturn.Phone = DocumentData.Trimmed(request.Phone);
        salesReturn.OfficeId = request.OfficeId;
        salesReturn.WarehouseId = request.WarehouseId;
        salesReturn.SalesForceId = request.SalesForceId;
        salesReturn.SalesForceName = await SalesData.EmployeeNameAsync(db, request.SalesForceId, cancellationToken);
        salesReturn.ReturnTypeId = request.ReturnTypeId;
        salesReturn.ReturnTypeName = Name(request.ReturnTypeId);
        salesReturn.ReturnReasonId = request.ReturnReasonId;
        salesReturn.ReturnReasonName = Name(request.ReturnReasonId);
        salesReturn.ReceivedById = request.ReceivedById;
        salesReturn.ReceivedByName = await SalesData.EmployeeNameAsync(db, request.ReceivedById, cancellationToken);
        salesReturn.ReturnRequestNo = DocumentData.Trimmed(request.ReturnRequestNo);
        salesReturn.ReturnRefNo = DocumentData.Trimmed(request.ReturnRefNo);
        salesReturn.ReturnRefDate = request.ReturnRefDate;
        salesReturn.ReturnRefAmount = request.ReturnRefAmount ?? 0;
        salesReturn.CreditNoteNo = DocumentData.Trimmed(request.CreditNoteNo);
        salesReturn.CreditNoteDate = request.CreditNoteDate;
        salesReturn.ChallanNo = DocumentData.Trimmed(request.ChallanNo);
        salesReturn.VatChallanNo = DocumentData.Trimmed(request.VatChallanNo);
        salesReturn.DeliveryMethodId = request.DeliveryMethodId;
        salesReturn.DeliveryMethodName = Name(request.DeliveryMethodId);
        salesReturn.PickupAddress = DocumentData.Trimmed(request.PickupAddress);
        salesReturn.IsExchange = request.IsExchange;
        salesReturn.IsVatInclusive = delivery.IsVatInclusive;
        salesReturn.TotalQuantity = lines.Sum(l => l.Quantity);
        salesReturn.ItemTotalAmount = lines.Sum(l => l.TotalAmount);
        salesReturn.TotalDiscount = lines.Sum(l => l.DiscountAmount);
        salesReturn.TotalSd = lines.Sum(l => l.SdAmount);
        salesReturn.TotalVat = lines.Sum(l => l.VatAmount);
        salesReturn.TotalAit = lines.Sum(l => l.AitAmount);
        salesReturn.SpecialDiscountAmount = special;
        salesReturn.ChargeRefundAmount = charge;
        salesReturn.DeductionLabel = DocumentData.Trimmed(request.DeductionLabel);
        salesReturn.DeductionValue = deduction;
        salesReturn.NetAmount = net;
        salesReturn.DueAdjusted = dueAdjusted;
        salesReturn.RefundAmount = refund;
        salesReturn.AdvanceAmount = net - dueAdjusted - refund;
        salesReturn.PaymentMethodId = request.PaymentMethodId;
        salesReturn.BankAccountId = request.BankAccountId;
        salesReturn.BankAccountName = await db.BankAccounts.Where(b => b.Id == request.BankAccountId).Select(b => b.Name)
            .FirstOrDefaultAsync(cancellationToken);
        salesReturn.InstrumentNo = DocumentData.Trimmed(request.InstrumentNo);
        salesReturn.InstrumentDate = request.InstrumentDate;
        salesReturn.TotalCost = lines.Sum(l => l.CostTotal);
        salesReturn.LossAmount = lines.Sum(l => l.LossAmount);
        salesReturn.Remarks = DocumentData.Trimmed(request.Remarks);
        salesReturn.Narration = DocumentData.Trimmed(request.Narration);
        salesReturn.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, salesReturn.Id, cancellationToken);
        await DeliveryDues.SyncAsync(db, [previousDelivery, delivery.Id], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, salesReturn.Id, cancellationToken))!);
    }

    private static List<SalesReturnLine> Lines(
        SalesDelivery delivery, Dictionary<int, ReturnedLine> returned, SaveSalesReturnRequest request, Dictionary<string, string[]> errors)
    {
        var lines = new List<SalesReturnLine>();
        var problems = new List<string>();
        if (request.Lines.DistinctBy(l => l.DeliveryLineId).Count() != request.Lines.Count) problems.Add("Each item can be added only once.");
        foreach (var line in request.Lines)
        {
            if (delivery.Lines.Find(l => l.Id == line.DeliveryLineId) is not { } delivered)
            {
                problems.Add("Every item must be on the delivery.");
                continue;
            }

            var before = returned.GetValueOrDefault(delivered.Id, ReturnedLine.None);
            var left = delivered.Quantity - before.Quantity;
            if (line.Quantity > left)
            {
                problems.Add($"Only {Math.Max(0, left):0.####} of {delivered.Item!.Code} can be returned.");
                continue;
            }

            var cost = DeliveryReturns.Cost(delivered, line.Quantity, before);
            lines.Add(new SalesReturnLine
            {
                Sequence = lines.Count + 1,
                DeliveryLineId = delivered.Id,
                ItemId = delivered.ItemId,
                ItemName = delivered.ItemName,
                ItemDescription = DocumentData.Trimmed(line.ItemDescription),
                UomId = delivered.UomId,
                UomName = delivered.UomName,
                Quantity = line.Quantity,
                Price = delivered.Price,
                IsAmountWiseDiscount = true,
                DiscountAmount = OrderDeliveries.Discount(delivered, line.Quantity, (before.Quantity, before.Discount)),
                SdPercentage = delivered.SdPercentage,
                VatPercentage = delivered.VatPercentage,
                AitPercentage = delivered.AitPercentage,
                WarrantyInMonth = delivered.WarrantyInMonth,
                IsOfferItem = delivered.IsOfferItem,
                IsDamaged = line.IsDamaged,
                CostTotal = cost,
                CostPrice = Math.Round(cost / line.Quantity, 4),
                LossAmount = line.IsDamaged ? cost : 0,
                Batch = DocumentData.Trimmed(line.Batch),
                ItemSerial = DocumentData.Trimmed(line.ItemSerial),
                Remarks = DocumentData.Trimmed(line.Remarks),
            });
        }

        if (problems.Count > 0) errors["lines"] = [.. problems.Distinct()];
        return lines;
    }

    private static SaveSalesReturnRequest ToRequest(SalesReturn r) => new()
    {
        Code = r.Code,
        ReturnDate = r.ReturnDate,
        DeliveryId = r.DeliveryId,
        WarehouseId = r.WarehouseId,
        OfficeId = r.OfficeId,
        SalesForceId = r.SalesForceId,
        Phone = r.Phone,
        ReturnTypeId = r.ReturnTypeId,
        ReturnReasonId = r.ReturnReasonId,
        ReceivedById = r.ReceivedById,
        ReturnRequestNo = r.ReturnRequestNo,
        ReturnRefNo = r.ReturnRefNo,
        ReturnRefDate = r.ReturnRefDate,
        ReturnRefAmount = r.ReturnRefAmount,
        CreditNoteNo = r.CreditNoteNo,
        CreditNoteDate = r.CreditNoteDate,
        ChallanNo = r.ChallanNo,
        VatChallanNo = r.VatChallanNo,
        DeliveryMethodId = r.DeliveryMethodId,
        PickupAddress = r.PickupAddress,
        IsExchange = r.IsExchange,
        ChargeRefundAmount = r.ChargeRefundAmount,
        DeductionLabel = r.DeductionLabel,
        DeductionValue = r.DeductionValue,
        RefundAmount = r.RefundAmount,
        PaymentMethodId = r.PaymentMethodId,
        BankAccountId = r.BankAccountId,
        InstrumentNo = r.InstrumentNo,
        InstrumentDate = r.InstrumentDate,
        Remarks = r.Remarks,
        Narration = r.Narration,
        Lines = [],
        CustomData = CustomFieldData.Parse(r.CustomData),
    };

    private static async Task<SalesReturnDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var r = await db.SalesReturns.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (r is null) return null;

        var delivery = await db.SalesDeliveries.AsNoTracking().Include(d => d.Lines).SingleAsync(d => d.Id == r.DeliveryId, cancellationToken);
        var delivered = delivery.Lines.ToDictionary(l => l.Id, l => l.Quantity);
        var returned = await DeliveryReturns.LinesAsync(db, [delivery.Id], id, cancellationToken);
        var due = await DeliveryDues.DueAsync(db, [delivery.Id], id, 0, cancellationToken);
        var names = await PartnerData.LookupNamesAsync(db, [r.PaymentMethodId], cancellationToken);

        return new SalesReturnDetail
        {
            Id = r.Id,
            Code = r.Code,
            ReturnDate = r.ReturnDate,
            DeliveryId = r.DeliveryId,
            DeliveryName = r.DeliveryCode,
            OrderId = r.OrderId,
            OrderName = r.OrderCode,
            CustomerId = r.CustomerId,
            CustomerName = r.CustomerName,
            Phone = r.Phone,
            OfficeId = r.OfficeId,
            OfficeName = await db.Offices.Where(o => o.Id == r.OfficeId).Select(o => o.Name).FirstOrDefaultAsync(cancellationToken),
            WarehouseId = r.WarehouseId,
            WarehouseName = await db.Warehouses.Where(w => w.Id == r.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            SalesForceId = r.SalesForceId,
            SalesForceName = r.SalesForceName,
            ReturnTypeId = r.ReturnTypeId,
            ReturnTypeName = r.ReturnTypeName,
            ReturnReasonId = r.ReturnReasonId,
            ReturnReasonName = r.ReturnReasonName,
            ReceivedById = r.ReceivedById,
            ReceivedByName = r.ReceivedByName,
            ReturnRequestNo = r.ReturnRequestNo,
            ReturnRefNo = r.ReturnRefNo,
            ReturnRefDate = r.ReturnRefDate,
            ReturnRefAmount = r.ReturnRefAmount,
            CreditNoteNo = r.CreditNoteNo,
            CreditNoteDate = r.CreditNoteDate,
            ChallanNo = r.ChallanNo,
            VatChallanNo = r.VatChallanNo,
            DeliveryMethodId = r.DeliveryMethodId,
            DeliveryMethodName = r.DeliveryMethodName,
            PickupAddress = r.PickupAddress,
            IsExchange = r.IsExchange,
            IsVatInclusive = r.IsVatInclusive,
            TotalQuantity = r.TotalQuantity,
            ItemTotalAmount = r.ItemTotalAmount,
            TotalDiscount = r.TotalDiscount,
            TotalSd = r.TotalSd,
            TotalVat = r.TotalVat,
            TotalAit = r.TotalAit,
            SpecialDiscountAmount = r.SpecialDiscountAmount,
            ChargeRefundAmount = r.ChargeRefundAmount,
            DeductionLabel = r.DeductionLabel,
            DeductionValue = r.DeductionValue,
            NetAmount = r.NetAmount,
            DueAdjusted = r.DueAdjusted,
            RefundAmount = r.RefundAmount,
            AdvanceAmount = r.AdvanceAmount,
            PaymentMethodId = r.PaymentMethodId,
            PaymentMethodName = r.PaymentMethodId is { } method ? names.GetValueOrDefault(method) : null,
            BankAccountId = r.BankAccountId,
            BankAccountName = r.BankAccountName,
            InstrumentNo = r.InstrumentNo,
            InstrumentDate = r.InstrumentDate,
            TotalCost = r.TotalCost,
            LossAmount = r.LossAmount,
            Remarks = r.Remarks,
            Narration = r.Narration,
            Status = ApprovalFlow.StatusKey(r.Status),
            ReviewedAt = r.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, r.ReviewedById, cancellationToken),
            ReviewComment = r.ReviewComment,
            Due = due[delivery.Id],
            ReductionRate = DeliveryReturns.ReductionRate(delivery),
            ChargeRefundable = await DeliveryReturns.ChargeRefundableAsync(db, delivery, id, cancellationToken),
            Lines = [.. r.Lines.OrderBy(l => l.Sequence).Select(l => new SalesReturnLineDetail(
                l.Id, l.DeliveryLineId, l.ItemId, l.Item!.Code, l.ItemName, l.ItemDescription, l.UomName, delivered[l.DeliveryLineId],
                delivered[l.DeliveryLineId] - returned.GetValueOrDefault(l.DeliveryLineId, ReturnedLine.None).Quantity, l.Quantity, l.Price,
                l.TotalAmount, l.DiscountAmount, l.SdPercentage, l.SdAmount, l.VatPercentage, l.VatAmount, l.AitPercentage, l.AitAmount,
                l.TotalValue, l.IsOfferItem, l.IsDamaged, l.CostTotal, l.LossAmount, l.Batch, l.ItemSerial, l.Remarks))],
            CustomData = CustomFieldData.Parse(r.CustomData),
            CreatedAt = r.CreatedAt,
            ModifiedAt = r.ModifiedAt,
        };
    }
}
