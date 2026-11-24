using Erp.Api.Accounting;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.PartnerManagement;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Procurement;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement.PurchasePayments;

internal static class PurchasePaymentEndpoints
{
    private const string Feature = ProcurementMenu.PurchasePayment;
    private const string CodePrefix = "PAY-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var payments = group.MapGroup("/purchase-payments");

        payments.MapGet("", List)
            .WithName("ListPurchasePayments")
            .WithSummary("Supplier payments in the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        payments.MapGet("/bills", Bills)
            .WithName("GetPayableBills")
            .WithSummary("What a supplier is owed: approved goods receives with an amount due and the advance left; paymentId leaves that payment out")
            .RequireAnyPermission(Editors);

        payments.MapGet("/{id:int}", Get)
            .WithName("GetPurchasePayment")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        payments.MapPost("", Create)
            .WithName("CreatePurchasePayment")
            .WithSummary("Pay approved bills of a supplier; an empty number gets the next PAY- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SavePurchasePaymentRequest>();

        payments.MapPut("/{id:int}", Update)
            .WithName("UpdatePurchasePayment")
            .WithSummary("Change a pending payment")
            .RequirePermission($"{Feature}.edit")
            .Validate<SavePurchasePaymentRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        payments.MapGet("/{id:int}/approval", Approval)
            .WithName("GetPurchasePaymentApproval")
            .WithSummary("Approval steps of a payment and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        payments.MapPost("/{id:int}/approve", Approve)
            .WithName("ApprovePurchasePayment")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        payments.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectPurchasePayment")
            .WithSummary("Reject a pending payment; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        payments.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelPurchasePayment")
            .WithSummary("Cancel an approved payment and take its journal back; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        payments.MapDelete("/{id:int}", Delete)
            .WithName("DeletePurchasePayment")
            .WithSummary("Delete a pending or rejected payment")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<PurchasePaymentList> List(
        string? search,
        string? status,
        int? supplierId,
        int? paymentMethodId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var payments = db.PurchasePayments.AsNoTracking().Where(p => p.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            payments = payments.Where(p => p.Code.Contains(term) ||
                (p.PayeeName != null && p.PayeeName.Contains(term)) ||
                (p.InstrumentNo != null && p.InstrumentNo.Contains(term)) ||
                (p.Remarks != null && p.Remarks.Contains(term)) ||
                db.Suppliers.Any(s => s.Id == p.SupplierId && s.Name.Contains(term)) ||
                p.Lines.Any(l => db.PurchaseReceives.Any(g => g.Id == l.PurchaseReceiveId && g.Code.Contains(term))));
        }

        if (supplierId is not null) payments = payments.Where(p => p.SupplierId == supplierId);
        if (paymentMethodId is not null) payments = payments.Where(p => p.PaymentMethodId == paymentMethodId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(payments, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(p => p.Code),
            "-code" => filtered.OrderByDescending(p => p.Code),
            "paymentDate" => filtered.OrderBy(p => p.PaymentDate).ThenBy(p => p.Id),
            _ => filtered.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.PaymentDate,
                p.SupplierId,
                SupplierName = db.Suppliers.Where(s => s.Id == p.SupplierId).Select(s => s.Name).FirstOrDefault(),
                p.PaymentMethodId,
                PaymentMethodName = db.LookupValues.Where(v => v.Id == p.PaymentMethodId).Select(v => v.NameEn).FirstOrDefault(),
                p.Status,
                BillCount = p.Lines.Count,
                p.PaymentAmount,
                p.IsAdvance,
                Settled = p.PaymentAmount + p.Vds + p.Tds + p.DiscountAmount + p.AdvanceAdjusted,
                p.CreatedAt,
                p.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new PurchasePaymentList(
            rows.Select(p => new PurchasePaymentRow(
                p.Id, p.Code, p.PaymentDate, p.SupplierId, p.SupplierName, p.PaymentMethodId, p.PaymentMethodName,
                ApprovalFlow.StatusKey(p.Status), p.BillCount, p.PaymentAmount, p.IsAdvance, p.Settled, p.CreatedAt, p.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<SupplierPayable> Bills(int supplierId, int? paymentId, ErpDbContext db, CancellationToken cancellationToken)
    {
        var receives = await db.PurchaseReceives.AsNoTracking()
            .Where(r => r.IsActive && r.Status == ApprovalStatus.Approved && r.SupplierId == supplierId)
            .OrderBy(r => r.ReceiveDate).ThenBy(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.ReceiveDate,
                PurchaseOrderName = db.PurchaseOrders.Where(o => o.Id == r.PurchaseOrderId).Select(o => o.Code).FirstOrDefault(),
                r.ChallanNo,
                r.NetAmount,
                r.ReturnAmount,
            })
            .ToListAsync(cancellationToken);

        var paid = await PayableBills.PaidAsync(db, receives.Select(r => r.Id).ToList(), paymentId ?? 0, cancellationToken);
        List<PayableBill> bills = [.. receives
            .Select(r =>
            {
                var paidAmount = paid.GetValueOrDefault(r.Id);
                return new PayableBill(r.Id, r.Code, r.ReceiveDate, r.PurchaseOrderName, r.ChallanNo, r.NetAmount, r.ReturnAmount,
                    paidAmount, r.NetAmount - r.ReturnAmount - paidAmount);
            })
            .Where(b => b.Due > 0)];
        return new SupplierPayable(bills, await SupplierAdvances.AvailableAsync(db, supplierId, paymentId ?? 0, cancellationToken));
    }

    private static async Task<Results<Ok<PurchasePaymentDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<PurchasePaymentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SavePurchasePaymentRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var payment = new PurchasePayment
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(payment, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<PurchasePaymentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SavePurchasePaymentRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var payment = await db.PurchasePayments.Include(p => p.Lines).SingleOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        if (payment is null) return TypedResults.NotFound();
        if (payment.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(payment, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var payment = await db.PurchasePayments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        return payment is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, payment, cancellationToken));
    }

    private static Task<Results<Ok<PurchasePaymentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, current, p => ApprovalFlow.ActAsync(db, current, Feature, p, true, request.Comment, cancellationToken), cancellationToken);

    private static async Task<Results<Ok<PurchasePaymentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, current, p => ApprovalFlow.ActAsync(db, current, Feature, p, false, request.Comment, cancellationToken),
                cancellationToken);

    private static async Task<Results<Ok<PurchasePaymentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, current, 
                d => ApprovalFlow.CancelAsync(db, current, Feature, d, request.Comment,
                    () => PurchaseCancel.PaymentAsync(db, current, d, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var payment = await db.PurchasePayments.Include(p => p.Lines).SingleOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        if (payment is null) return TypedResults.NotFound();
        if (DocumentData.Delete(payment) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await PayableBills.SyncAsync(db, payment.Lines.Select(l => l.PurchaseReceiveId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<PurchasePaymentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, ICurrentContext current, Func<PurchasePayment, Task<ProblemHttpResult?>> change,
        CancellationToken cancellationToken)
    {
        var payment = await db.PurchasePayments.Include(p => p.Lines).SingleOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        if (payment is null) return TypedResults.NotFound();
        if (await change(payment) is { } problem) return problem;
        if (payment.Status == ApprovalStatus.Approved && await PurchasePosting.PaymentAsync(db, current, payment, cancellationToken) is { } refused) return refused;
        await db.SaveChangesAsync(cancellationToken);
        await PayableBills.SyncAsync(db, payment.Lines.Select(l => l.PurchaseReceiveId), cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<PurchasePaymentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        PurchasePayment payment, SavePurchasePaymentRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = payment.Id > 0 ? ToRequest(payment) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await DocumentData.CodeAsync(db.PurchasePayments, payment, request.Code, errors, cancellationToken);
        await PeriodLock.CheckAsync(db, "paymentDate", request.PaymentDate, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
            [("paymentMethodId", request.PaymentMethodId, existing?.PaymentMethodId, ProcurementLookups.PaymentMethod)], errors,
            cancellationToken);
        await PaymentSources.CheckAsync(db, request.PaymentMethodId, request.BankAccountId, existing?.BankAccountId, errors, cancellationToken);
        if (request.SupplierId != existing?.SupplierId &&
            !await db.Suppliers.AnyAsync(s => s.Id == request.SupplierId && s.IsActive, cancellationToken))
        {
            errors["supplierId"] = ["Choose an active supplier of this business unit."];
        }

        var receiveIds = request.Lines.Select(l => l.PurchaseReceiveId).Distinct().ToList();
        if (receiveIds.Count != request.Lines.Count) errors["lines"] = ["Each bill can be added only once."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var kept = payment.Lines.Select(l => l.PurchaseReceiveId).ToHashSet();
        var receives = await db.PurchaseReceives.AsNoTracking()
            .Where(r => receiveIds.Contains(r.Id) && r.IsActive)
            .ToDictionaryAsync(r => r.Id, cancellationToken);
        var paid = await PayableBills.PaidAsync(db, receiveIds, payment.Id, cancellationToken);
        var problems = new List<string>();
        foreach (var line in request.Lines)
        {
            if (!receives.TryGetValue(line.PurchaseReceiveId, out var receive) || receive.SupplierId != request.SupplierId ||
                (receive.Status != ApprovalStatus.Approved && !kept.Contains(receive.Id)))
            {
                problems.Add("Every bill must be an approved goods receive of this supplier.");
                continue;
            }

            var due = receive.NetAmount - receive.ReturnAmount - paid.GetValueOrDefault(receive.Id);
            var settled = line.PaymentAmount + (line.Vds ?? 0) + (line.Tds ?? 0) + (line.DiscountAmount ?? 0) + (line.AdvanceAdjusted ?? 0);
            if (settled > due) problems.Add($"Only {Math.Max(0, due):0.##} is due on {receive.Code}.");
        }

        var adjusted = request.Lines.Sum(l => l.AdvanceAdjusted ?? 0);
        if (adjusted > 0)
        {
            var available = await SupplierAdvances.AvailableAsync(db, request.SupplierId, payment.Id, cancellationToken);
            if (adjusted > available) problems.Add($"Only {Math.Max(0, available):0.##} of advance is left for this supplier.");
        }

        if (problems.Count > 0) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["lines"] = [.. problems.Distinct()] });

        var lines = request.Lines.Select((line, index) => new PurchasePaymentLine
        {
            Sequence = index + 1,
            PurchaseReceiveId = line.PurchaseReceiveId,
            PaymentAmount = line.PaymentAmount,
            Vds = line.Vds ?? 0,
            Tds = line.Tds ?? 0,
            DiscountAmount = line.DiscountAmount ?? 0,
            AdvanceAdjusted = line.AdvanceAdjusted ?? 0,
        }).ToList();

        payment.Lines.Clear();
        payment.Lines.AddRange(lines);
        payment.IsAdvance = request.IsAdvance;
        payment.PaymentAmount = request.IsAdvance ? request.AdvanceAmount ?? 0 : lines.Sum(l => l.PaymentAmount);
        payment.Vds = lines.Sum(l => l.Vds);
        payment.Tds = lines.Sum(l => l.Tds);
        payment.DiscountAmount = lines.Sum(l => l.DiscountAmount);
        payment.AdvanceAdjusted = lines.Sum(l => l.AdvanceAdjusted);

        if (payment.Id == 0) db.PurchasePayments.Add(payment);
        payment.Code = code ?? await DocumentData.NextCodeAsync(db.PurchasePayments, CodePrefix, cancellationToken);
        payment.PaymentDate = request.PaymentDate;
        payment.SupplierId = request.SupplierId;
        payment.PaymentMethodId = request.PaymentMethodId;
        payment.BankAccountId = request.BankAccountId;
        payment.PayeeName = DocumentData.Trimmed(request.PayeeName);
        payment.BankAccountNo = DocumentData.Trimmed(request.BankAccountNo);
        payment.InstrumentNo = DocumentData.Trimmed(request.InstrumentNo);
        payment.InstrumentDate = request.InstrumentDate;
        payment.Remarks = DocumentData.Trimmed(request.Remarks);
        payment.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, payment.Id, cancellationToken);
        await PayableBills.SyncAsync(db, [.. kept, .. receiveIds], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, payment.Id, cancellationToken))!);
    }

    private static SavePurchasePaymentRequest ToRequest(PurchasePayment p) => new()
    {
        Code = p.Code,
        PaymentDate = p.PaymentDate,
        SupplierId = p.SupplierId,
        IsAdvance = p.IsAdvance,
        PaymentMethodId = p.PaymentMethodId,
        BankAccountId = p.BankAccountId,
        PayeeName = p.PayeeName,
        BankAccountNo = p.BankAccountNo,
        InstrumentNo = p.InstrumentNo,
        InstrumentDate = p.InstrumentDate,
        Remarks = p.Remarks,
        AdvanceAmount = p.IsAdvance ? p.PaymentAmount : null,
        Lines = [.. p.Lines.OrderBy(l => l.Sequence).Select(l => new SavePurchasePaymentLine
        {
            PurchaseReceiveId = l.PurchaseReceiveId,
            PaymentAmount = l.PaymentAmount,
            Vds = l.Vds,
            Tds = l.Tds,
            DiscountAmount = l.DiscountAmount,
            AdvanceAdjusted = l.AdvanceAdjusted,
        })],
        CustomData = CustomFieldData.Parse(p.CustomData),
    };

    private static async Task<PurchasePaymentDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var p = await db.PurchasePayments.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (p is null) return null;

        var receiveIds = p.Lines.Select(l => l.PurchaseReceiveId).ToList();
        var receives = await db.PurchaseReceives.AsNoTracking()
            .Where(r => receiveIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.ReceiveDate,
                PurchaseOrderName = db.PurchaseOrders.Where(o => o.Id == r.PurchaseOrderId).Select(o => o.Code).FirstOrDefault(),
                Bill = r.NetAmount - r.ReturnAmount,
            })
            .ToDictionaryAsync(r => r.Id, cancellationToken);
        var paid = await PayableBills.PaidAsync(db, receiveIds, p.Id, cancellationToken);
        var names = await PartnerData.LookupNamesAsync(db, [p.PaymentMethodId], cancellationToken);

        return new PurchasePaymentDetail
        {
            Id = p.Id,
            Code = p.Code,
            PaymentDate = p.PaymentDate,
            SupplierId = p.SupplierId,
            SupplierName = await db.Suppliers.Where(s => s.Id == p.SupplierId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken),
            PaymentMethodId = p.PaymentMethodId,
            PaymentMethodName = p.PaymentMethodId is { } method ? names.GetValueOrDefault(method) : null,
            IsAdvance = p.IsAdvance,
            BankAccountId = p.BankAccountId,
            BankAccountName = await db.BankAccounts.Where(b => b.Id == p.BankAccountId).Select(b => b.Name).FirstOrDefaultAsync(cancellationToken),
            PayeeName = p.PayeeName,
            BankAccountNo = p.BankAccountNo,
            InstrumentNo = p.InstrumentNo,
            InstrumentDate = p.InstrumentDate,
            Remarks = p.Remarks,
            Status = ApprovalFlow.StatusKey(p.Status),
            ReviewedAt = p.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, p.ReviewedById, cancellationToken),
            ReviewComment = p.ReviewComment,
            PaymentAmount = p.PaymentAmount,
            Vds = p.Vds,
            Tds = p.Tds,
            DiscountAmount = p.DiscountAmount,
            AdvanceAdjusted = p.AdvanceAdjusted,
            SettledAmount = p.PaymentAmount + p.Vds + p.Tds + p.DiscountAmount + p.AdvanceAdjusted,
            Lines = [.. p.Lines.OrderBy(l => l.Sequence).Select(l =>
            {
                var receive = receives[l.PurchaseReceiveId];
                return new PurchasePaymentLineDetail(
                    l.Id, l.PurchaseReceiveId, receive.Code, receive.ReceiveDate, receive.PurchaseOrderName, receive.Bill,
                    receive.Bill - paid.GetValueOrDefault(l.PurchaseReceiveId), l.PaymentAmount, l.Vds, l.Tds, l.DiscountAmount, l.AdvanceAdjusted,
                    l.PaymentAmount + l.Vds + l.Tds + l.DiscountAmount + l.AdvanceAdjusted);
            })],
            CustomData = CustomFieldData.Parse(p.CustomData),
            CreatedAt = p.CreatedAt,
            ModifiedAt = p.ModifiedAt,
        };
    }
}
