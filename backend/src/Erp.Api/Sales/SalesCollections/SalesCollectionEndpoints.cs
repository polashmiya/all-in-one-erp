using Erp.Api.Accounting;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.PartnerManagement;
using Erp.Api.Procurement;
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

namespace Erp.Api.Sales.SalesCollections;

internal static class SalesCollectionEndpoints
{
    private const string Feature = SalesMenu.SalesCollection;
    private const string CodePrefix = "SC-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var collections = group.MapGroup("/sales-collections");

        collections.MapGet("", List)
            .WithName("ListSalesCollections")
            .WithSummary("Sales collections of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        collections.MapGet("/bills", Bills)
            .WithName("GetCollectableBills")
            .WithSummary("Approved deliveries of a customer that are still due, and the advance that can be used")
            .RequireAnyPermission(Editors);

        collections.MapGet("/{id:int}", Get)
            .WithName("GetSalesCollection")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        collections.MapPost("", Create)
            .WithName("CreateSalesCollection")
            .WithSummary("Create a pending collection or advance; an empty number gets the next SC- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveSalesCollectionRequest>();

        collections.MapPut("/{id:int}", Update)
            .WithName("UpdateSalesCollection")
            .WithSummary("Change a pending sales collection")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveSalesCollectionRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        collections.MapGet("/{id:int}/approval", Approval)
            .WithName("GetSalesCollectionApproval")
            .WithSummary("Approval steps of a sales collection and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        collections.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveSalesCollection")
            .WithSummary("Approve a collection; the last approval posts the accounts")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        collections.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectSalesCollection")
            .WithSummary("Reject a pending collection; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        collections.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelSalesCollection")
            .WithSummary("Cancel an approved collection, for example a bounced cheque; the accounts are reversed")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        collections.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSalesCollection")
            .WithSummary("Delete a pending or rejected sales collection")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<SalesCollectionList> List(
        string? search,
        string? status,
        int? customerId,
        int? paymentMethodId,
        int? collectedById,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var collections = db.SalesCollections.AsNoTracking().Where(c => c.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            collections = collections.Where(c => c.Code.Contains(term) || c.CustomerName.Contains(term) ||
                (c.MoneyReceiptNo != null && c.MoneyReceiptNo.Contains(term)) ||
                (c.InstrumentNo != null && c.InstrumentNo.Contains(term)) ||
                (c.ReferenceNo != null && c.ReferenceNo.Contains(term)) ||
                c.Lines.Any(l => l.DeliveryCode.Contains(term)));
        }

        if (customerId is not null) collections = collections.Where(c => c.CustomerId == customerId);
        if (paymentMethodId is not null) collections = collections.Where(c => c.PaymentMethodId == paymentMethodId);
        if (collectedById is not null) collections = collections.Where(c => c.CollectedById == collectedById);

        var (filtered, counts) = await DocumentData.ByStatusAsync(collections, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(c => c.Code),
            "-code" => filtered.OrderByDescending(c => c.Code),
            "collectionDate" => filtered.OrderBy(c => c.CollectionDate).ThenBy(c => c.Id),
            _ => filtered.OrderByDescending(c => c.CollectionDate).ThenByDescending(c => c.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(c => new { Collection = c, BillCount = c.Lines.Count })
            .ToListAsync(cancellationToken);

        return new SalesCollectionList(
            rows.Select(x => new SalesCollectionRow(
                x.Collection.Id, x.Collection.Code, x.Collection.CollectionDate, x.Collection.CustomerId, x.Collection.CustomerName,
                x.Collection.IsAdvance, x.Collection.PaymentMethodId, x.Collection.PaymentMethodName, x.Collection.MoneyReceiptNo,
                x.Collection.CollectedById, x.Collection.CollectedByName, ApprovalFlow.StatusKey(x.Collection.Status), x.BillCount,
                x.Collection.CollectionAmount, x.Collection.SettledAmount, x.Collection.CreatedAt, x.Collection.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<CustomerReceivable> Bills(int customerId, int? collectionId, ErpDbContext db, CancellationToken cancellationToken)
    {
        var deliveries = await db.SalesDeliveries.AsNoTracking()
            .Where(d => d.IsActive && d.Status == ApprovalStatus.Approved && d.CustomerId == customerId)
            .OrderBy(d => d.DeliveryDate).ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);
        var due = await DeliveryDues.DueAsync(db, deliveries.Select(d => d.Id).ToList(), 0, collectionId ?? 0, cancellationToken);
        List<CollectableBill> bills = [.. deliveries
            .Select(d => new CollectableBill(
                d.Id, d.Code, d.DeliveryDate, d.OrderCode, d.ChallanNo, d.PaymentTermDays, d.DeliveryDate.AddDays(d.PaymentTermDays),
                d.NetAmount, d.ReturnAdjusted, d.NetAmount - d.ReturnAdjusted - due[d.Id], due[d.Id]))
            .Where(b => b.Due > 0)];
        return new CustomerReceivable(bills, await CustomerAdvances.AvailableAsync(db, customerId, collectionId ?? 0, cancellationToken));
    }

    private static async Task<Results<Ok<SalesCollectionDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<SalesCollectionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveSalesCollectionRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var collection = new SalesCollection
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(collection, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SalesCollectionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveSalesCollectionRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var collection = await FindAsync(db, id, cancellationToken);
        if (collection is null) return TypedResults.NotFound();
        if (collection.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(collection, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var collection = await db.SalesCollections.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);
        return collection is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, collection, cancellationToken));
    }

    private static Task<Results<Ok<SalesCollectionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async c =>
            await ApprovalFlow.ActAsync(db, current, Feature, c, true, request.Comment, cancellationToken) ??
            (c.Status == ApprovalStatus.Approved ? await SalesPosting.CollectionAsync(db, current, c, cancellationToken) : null),
            cancellationToken);

    private static async Task<Results<Ok<SalesCollectionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, c => ApprovalFlow.ActAsync(db, current, Feature, c, false, request.Comment, cancellationToken),
                cancellationToken);

    private static async Task<Results<Ok<SalesCollectionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db,
                c => ApprovalFlow.CancelAsync(db, current, Feature, c, request.Comment,
                    () => SalesCancel.CollectionAsync(db, current, c, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var collection = await FindAsync(db, id, cancellationToken);
        if (collection is null) return TypedResults.NotFound();
        if (DocumentData.Delete(collection) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await DeliveryDues.SyncAsync(db, collection.Lines.Select(l => l.DeliveryId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<SalesCollection?> FindAsync(ErpDbContext db, int id, CancellationToken cancellationToken) =>
        db.SalesCollections.Include(c => c.Lines).SingleOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

    private static async Task<Results<Ok<SalesCollectionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<SalesCollection, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var collection = await FindAsync(db, id, cancellationToken);
        if (collection is null) return TypedResults.NotFound();
        if (await change(collection) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        await DeliveryDues.SyncAsync(db, collection.Lines.Select(l => l.DeliveryId), cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<SalesCollectionDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        SalesCollection collection, SaveSalesCollectionRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = collection.Id > 0 ? ToRequest(collection) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await DocumentData.CodeAsync(db.SalesCollections, collection, request.Code, errors, cancellationToken);
        await PeriodLock.CheckAsync(db, "collectionDate", request.CollectionDate, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("paymentMethodId", request.PaymentMethodId, existing?.PaymentMethodId, ProcurementLookups.PaymentMethod),
            ("instrumentTypeId", request.InstrumentTypeId, existing?.InstrumentTypeId, SalesLookups.InstrumentType),
            ("currencyId", request.CurrencyId, existing?.CurrencyId, SalesLookups.Currency),
        ], errors, cancellationToken);
        await PaymentSources.CheckAsync(
            db, request.PaymentMethodId, request.BankAccountId, existing?.BankAccountId, errors, cancellationToken);
        await SalesData.CheckPeopleAsync(
            db, request.CustomerId, existing?.CustomerId, request.OfficeId, existing?.OfficeId, null, null, errors, cancellationToken);
        await SalesData.CheckEmployeeAsync(db, "collectedById", request.CollectedById, existing?.CollectedById, errors, cancellationToken);

        var deliveryIds = request.Lines.Select(l => l.DeliveryId).Distinct().ToList();
        if (deliveryIds.Count != request.Lines.Count) errors["lines"] = ["Each bill can be added only once."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var deliveries = await db.SalesDeliveries.AsNoTracking()
            .Where(d => deliveryIds.Contains(d.Id) && d.IsActive && d.Status == ApprovalStatus.Approved && d.CustomerId == request.CustomerId)
            .ToDictionaryAsync(d => d.Id, cancellationToken);
        var due = await DeliveryDues.DueAsync(db, deliveryIds, 0, collection.Id, cancellationToken);
        var problems = new List<string>();
        var lines = new List<SalesCollectionLine>();
        foreach (var line in request.Lines)
        {
            if (!deliveries.TryGetValue(line.DeliveryId, out var delivery))
            {
                problems.Add("Every bill must be an approved delivery of this customer.");
                continue;
            }

            var saved = Line(line, delivery, lines.Count + 1);
            if (saved.SettledAmount <= 0) problems.Add($"Enter an amount for {delivery.Code}.");
            else if (saved.SettledAmount > due[delivery.Id]) problems.Add($"Only {Math.Max(0, due[delivery.Id]):0.##} is due on {delivery.Code}.");
            lines.Add(saved);
        }

        var adjusted = lines.Sum(l => l.AdvanceAdjusted);
        if (adjusted > 0)
        {
            var available = await CustomerAdvances.AvailableAsync(db, request.CustomerId, collection.Id, cancellationToken);
            if (adjusted > available) problems.Add($"Only {Math.Max(0, available):0.##} of advance is left for this customer.");
        }

        if (problems.Count > 0) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["lines"] = [.. problems.Distinct()] });

        var previous = collection.Lines.Select(l => l.DeliveryId).ToList();
        if (collection.CustomerId != request.CustomerId)
        {
            collection.CustomerName = await db.Customers.Where(c => c.Id == request.CustomerId).Select(c => c.Name)
                .SingleAsync(cancellationToken);
        }

        collection.Lines.Clear();
        collection.Lines.AddRange(lines);
        if (collection.Id == 0) db.SalesCollections.Add(collection);
        var names = await PartnerData.LookupNamesAsync(
            db, [request.PaymentMethodId, request.InstrumentTypeId, request.CurrencyId], cancellationToken);
        string? Name(int? id) => id is { } key ? names.GetValueOrDefault(key) : null;

        collection.Code = code ?? await DocumentData.NextCodeAsync(db.SalesCollections, CodePrefix, cancellationToken);
        collection.CollectionDate = request.CollectionDate;
        collection.CustomerId = request.CustomerId;
        collection.IsAdvance = request.IsAdvance;
        collection.OfficeId = request.OfficeId;
        collection.CollectedById = request.CollectedById;
        collection.CollectedByName = await SalesData.EmployeeNameAsync(db, request.CollectedById, cancellationToken);
        collection.MoneyReceiptNo = DocumentData.Trimmed(request.MoneyReceiptNo);
        collection.ReferenceNo = DocumentData.Trimmed(request.ReferenceNo);
        collection.PaymentMethodId = request.PaymentMethodId;
        collection.PaymentMethodName = Name(request.PaymentMethodId);
        collection.BankAccountId = request.BankAccountId;
        collection.BankAccountName = await db.BankAccounts.Where(b => b.Id == request.BankAccountId).Select(b => b.Name)
            .FirstOrDefaultAsync(cancellationToken);
        collection.CustomerBankName = DocumentData.Trimmed(request.CustomerBankName);
        collection.CustomerBankBranch = DocumentData.Trimmed(request.CustomerBankBranch);
        collection.InstrumentTypeId = request.InstrumentTypeId;
        collection.InstrumentTypeName = Name(request.InstrumentTypeId);
        collection.InstrumentNo = DocumentData.Trimmed(request.InstrumentNo);
        collection.InstrumentDate = request.InstrumentDate;
        collection.DepositDate = request.DepositDate;
        collection.TransactionNo = DocumentData.Trimmed(request.TransactionNo);
        collection.VdsCertificateNo = DocumentData.Trimmed(request.VdsCertificateNo);
        collection.VdsCertificateDate = request.VdsCertificateDate;
        collection.AitCertificateNo = DocumentData.Trimmed(request.AitCertificateNo);
        collection.AitCertificateDate = request.AitCertificateDate;
        collection.CurrencyId = request.CurrencyId;
        collection.CurrencyName = Name(request.CurrencyId);
        collection.ExchangeRate = request.ExchangeRate ?? 0;
        collection.Remarks = DocumentData.Trimmed(request.Remarks);
        collection.Narration = DocumentData.Trimmed(request.Narration);
        collection.CollectionAmount = request.IsAdvance ? request.AdvanceAmount ?? 0 : lines.Sum(l => l.CollectionAmount);
        collection.Vds = lines.Sum(l => l.Vds);
        collection.Ait = lines.Sum(l => l.Ait);
        collection.DiscountAmount = lines.Sum(l => l.DiscountAmount);
        collection.ExpenseAmount = lines.Sum(l => l.ExpenseAmount);
        collection.AdvanceAdjusted = adjusted;
        collection.SettledAmount = request.IsAdvance ? collection.CollectionAmount : lines.Sum(l => l.SettledAmount);
        collection.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, collection.Id, cancellationToken);
        await DeliveryDues.SyncAsync(db, [.. previous, .. deliveryIds], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, collection.Id, cancellationToken))!);
    }

    private static SalesCollectionLine Line(SaveSalesCollectionLine line, SalesDelivery delivery, int sequence)
    {
        var saved = new SalesCollectionLine
        {
            Sequence = sequence,
            DeliveryId = delivery.Id,
            DeliveryCode = delivery.Code,
            CollectionAmount = line.CollectionAmount,
            Vds = line.Vds ?? 0,
            Ait = line.Ait ?? 0,
            DiscountAmount = line.DiscountAmount ?? 0,
            ExpenseAmount = line.ExpenseAmount ?? 0,
            AdvanceAdjusted = line.AdvanceAdjusted ?? 0,
            Remarks = DocumentData.Trimmed(line.Remarks),
        };
        saved.SettledAmount =
            saved.CollectionAmount + saved.Vds + saved.Ait + saved.DiscountAmount + saved.ExpenseAmount + saved.AdvanceAdjusted;
        return saved;
    }

    private static SaveSalesCollectionRequest ToRequest(SalesCollection c) => new()
    {
        Code = c.Code,
        CollectionDate = c.CollectionDate,
        CustomerId = c.CustomerId,
        IsAdvance = c.IsAdvance,
        AdvanceAmount = c.IsAdvance ? c.CollectionAmount : null,
        OfficeId = c.OfficeId,
        CollectedById = c.CollectedById,
        MoneyReceiptNo = c.MoneyReceiptNo,
        ReferenceNo = c.ReferenceNo,
        PaymentMethodId = c.PaymentMethodId,
        BankAccountId = c.BankAccountId,
        CustomerBankName = c.CustomerBankName,
        CustomerBankBranch = c.CustomerBankBranch,
        InstrumentTypeId = c.InstrumentTypeId,
        InstrumentNo = c.InstrumentNo,
        InstrumentDate = c.InstrumentDate,
        DepositDate = c.DepositDate,
        TransactionNo = c.TransactionNo,
        VdsCertificateNo = c.VdsCertificateNo,
        VdsCertificateDate = c.VdsCertificateDate,
        AitCertificateNo = c.AitCertificateNo,
        AitCertificateDate = c.AitCertificateDate,
        CurrencyId = c.CurrencyId,
        ExchangeRate = c.ExchangeRate,
        Remarks = c.Remarks,
        Narration = c.Narration,
        Lines = [],
        CustomData = CustomFieldData.Parse(c.CustomData),
    };

    private static async Task<SalesCollectionDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var c = await db.SalesCollections.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (c is null) return null;

        var deliveryIds = c.Lines.Select(l => l.DeliveryId).ToList();
        var deliveries = await db.SalesDeliveries.AsNoTracking()
            .Where(d => deliveryIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);
        var due = await DeliveryDues.DueAsync(db, deliveryIds, 0, id, cancellationToken);

        return new SalesCollectionDetail
        {
            Id = c.Id,
            Code = c.Code,
            CollectionDate = c.CollectionDate,
            CustomerId = c.CustomerId,
            CustomerName = c.CustomerName,
            IsAdvance = c.IsAdvance,
            AdvanceAmount = c.IsAdvance ? c.CollectionAmount : null,
            OfficeId = c.OfficeId,
            OfficeName = await db.Offices.Where(o => o.Id == c.OfficeId).Select(o => o.Name).FirstOrDefaultAsync(cancellationToken),
            CollectedById = c.CollectedById,
            CollectedByName = c.CollectedByName,
            MoneyReceiptNo = c.MoneyReceiptNo,
            ReferenceNo = c.ReferenceNo,
            PaymentMethodId = c.PaymentMethodId,
            PaymentMethodName = c.PaymentMethodName,
            BankAccountId = c.BankAccountId,
            BankAccountName = c.BankAccountName,
            CustomerBankName = c.CustomerBankName,
            CustomerBankBranch = c.CustomerBankBranch,
            InstrumentTypeId = c.InstrumentTypeId,
            InstrumentTypeName = c.InstrumentTypeName,
            InstrumentNo = c.InstrumentNo,
            InstrumentDate = c.InstrumentDate,
            DepositDate = c.DepositDate,
            TransactionNo = c.TransactionNo,
            VdsCertificateNo = c.VdsCertificateNo,
            VdsCertificateDate = c.VdsCertificateDate,
            AitCertificateNo = c.AitCertificateNo,
            AitCertificateDate = c.AitCertificateDate,
            CurrencyId = c.CurrencyId,
            CurrencyName = c.CurrencyName,
            ExchangeRate = c.ExchangeRate,
            Remarks = c.Remarks,
            Narration = c.Narration,
            Status = ApprovalFlow.StatusKey(c.Status),
            ReviewedAt = c.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, c.ReviewedById, cancellationToken),
            ReviewComment = c.ReviewComment,
            CollectionAmount = c.CollectionAmount,
            Vds = c.Vds,
            Ait = c.Ait,
            DiscountAmount = c.DiscountAmount,
            ExpenseAmount = c.ExpenseAmount,
            AdvanceAdjusted = c.AdvanceAdjusted,
            SettledAmount = c.SettledAmount,
            Lines = [.. c.Lines.OrderBy(l => l.Sequence).Select(l => new SalesCollectionLineDetail(
                l.Id, l.DeliveryId, l.DeliveryCode, deliveries[l.DeliveryId].DeliveryDate, deliveries[l.DeliveryId].OrderCode,
                deliveries[l.DeliveryId].NetAmount, due[l.DeliveryId], l.CollectionAmount, l.Vds, l.Ait, l.DiscountAmount, l.ExpenseAmount,
                l.AdvanceAdjusted, l.SettledAmount, l.Remarks))],
            CustomData = CustomFieldData.Parse(c.CustomData),
            CreatedAt = c.CreatedAt,
            ModifiedAt = c.ModifiedAt,
        };
    }
}
