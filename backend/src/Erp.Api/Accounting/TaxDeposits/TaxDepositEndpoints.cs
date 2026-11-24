using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.Inventory;
using Erp.Api.PartnerManagement;
using Erp.Api.Procurement;
using Erp.Core.Authorization;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Approvals;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting.TaxDeposits;

internal static class TaxDepositEndpoints
{
    private const string Feature = AccountingMenu.TaxDeposit;
    private const string CodePrefix = "DEP-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var deposits = group.MapGroup("/tax-deposits");

        deposits.MapGet("", List)
            .WithName("ListTaxDeposits")
            .WithSummary("VDS and TDS deposits of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        deposits.MapGet("/pending", Pending)
            .WithName("GetPendingTaxes")
            .WithSummary("Approved payments with VDS or TDS that is not deposited yet; depositId leaves that deposit out")
            .RequireAnyPermission(Editors);

        deposits.MapGet("/{id:int}", Get)
            .WithName("GetTaxDeposit")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        deposits.MapPost("", Create)
            .WithName("CreateTaxDeposit")
            .WithSummary("Deposit VDS or TDS; an empty number gets the next DEP- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveTaxDepositRequest>();

        deposits.MapPut("/{id:int}", Update)
            .WithName("UpdateTaxDeposit")
            .WithSummary("Change a pending deposit")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveTaxDepositRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deposits.MapGet("/{id:int}/approval", Approval)
            .WithName("GetTaxDepositApproval")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        deposits.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveTaxDeposit")
            .WithSummary("The last approval posts the journal")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deposits.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectTaxDeposit")
            .WithSummary("Reject a pending deposit; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deposits.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelTaxDeposit")
            .WithSummary("Cancel an approved deposit and take its journal back; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        deposits.MapDelete("/{id:int}", Delete)
            .WithName("DeleteTaxDeposit")
            .WithSummary("Delete a pending or rejected deposit")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static string TypeKey(TaxType type) => type.ToString().ToLowerInvariant();

    private static async Task<TaxDepositList> List(
        string? search, string? status, string? type, string? sort, int? page, int? pageSize, ErpDbContext db, CancellationToken cancellationToken)
    {
        var deposits = db.TaxDeposits.AsNoTracking().Where(d => d.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            deposits = deposits.Where(d => d.Code.Contains(term) ||
                (d.ChallanNo != null && d.ChallanNo.Contains(term)) ||
                (d.Remarks != null && d.Remarks.Contains(term)) ||
                d.Lines.Any(l => db.PurchasePayments.Any(p => p.Id == l.PurchasePaymentId && p.Code.Contains(term))));
        }

        if (Enum.TryParse<TaxType>(type, true, out var only)) deposits = deposits.Where(d => d.Type == only);

        var (filtered, counts) = await DocumentData.ByStatusAsync(deposits, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(d => d.Code),
            "-code" => filtered.OrderByDescending(d => d.Code),
            "depositDate" => filtered.OrderBy(d => d.DepositDate).ThenBy(d => d.Id),
            _ => filtered.OrderByDescending(d => d.DepositDate).ThenByDescending(d => d.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.DepositDate,
                d.Type,
                d.PaymentMethodId,
                PaymentMethodName = db.LookupValues.Where(v => v.Id == d.PaymentMethodId).Select(v => v.NameEn).FirstOrDefault(),
                d.ChallanNo,
                d.Status,
                PaymentCount = d.Lines.Count,
                d.TotalAmount,
                d.CreatedAt,
                d.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new TaxDepositList(
            [.. rows.Select(d => new TaxDepositRow(
                d.Id, d.Code, d.DepositDate, TypeKey(d.Type), d.PaymentMethodId, d.PaymentMethodName, d.ChallanNo,
                ApprovalFlow.StatusKey(d.Status), d.PaymentCount, d.TotalAmount, d.CreatedAt, d.ModifiedAt))],
            total,
            counts);
    }

    private static async Task<Results<Ok<List<PendingTax>>, ValidationProblem>> Pending(
        string type, int? depositId, ErpDbContext db, CancellationToken cancellationToken) =>
        Enum.TryParse<TaxType>(type, true, out var only)
            ? TypedResults.Ok((await TaxesAsync(db, only, null, depositId ?? 0, cancellationToken)).Where(t => t.Remaining > 0).ToList())
            : TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["type"] = ["Choose VDS or TDS."] });

    private static async Task<List<PendingTax>> TaxesAsync(
        ErpDbContext db, TaxType type, IReadOnlyCollection<int>? paymentIds, int exceptDepositId, CancellationToken cancellationToken)
    {
        var payments = await db.PurchasePayments.AsNoTracking()
            .Where(p => p.IsActive && p.Status == ApprovalStatus.Approved && (type == TaxType.Vds ? p.Vds : p.Tds) > 0 &&
                (paymentIds == null || paymentIds.Contains(p.Id)))
            .OrderBy(p => p.PaymentDate).ThenBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.PaymentDate,
                SupplierName = db.Suppliers.Where(s => s.Id == p.SupplierId).Select(s => s.Name).FirstOrDefault(),
                Deducted = type == TaxType.Vds ? p.Vds : p.Tds,
            })
            .ToListAsync(cancellationToken);
        var deposited = await db.TaxDeposits.AsNoTracking()
            .Where(d => d.IsActive && d.Type == type && d.Id != exceptDepositId &&
                d.Status != ApprovalStatus.Rejected && d.Status != ApprovalStatus.Cancelled)
            .SelectMany(d => d.Lines)
            .GroupBy(l => l.PurchasePaymentId)
            .Select(g => new { g.Key, Amount = g.Sum(l => l.Amount) })
            .ToDictionaryAsync(g => g.Key, g => g.Amount, cancellationToken);
        return [.. payments.Select(p =>
        {
            var amount = deposited.GetValueOrDefault(p.Id);
            return new PendingTax(p.Id, p.Code, p.PaymentDate, p.SupplierName, p.Deducted, amount, p.Deducted - amount);
        })];
    }

    private static async Task<Results<Ok<TaxDepositDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<TaxDepositDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveTaxDepositRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var deposit = new TaxDeposit
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(deposit, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<TaxDepositDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveTaxDepositRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var deposit = await db.TaxDeposits.Include(d => d.Lines).SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (deposit is null) return TypedResults.NotFound();
        if (deposit.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(deposit, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var deposit = await db.TaxDeposits.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        return deposit is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, deposit, cancellationToken));
    }

    private static Task<Results<Ok<TaxDepositDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async d =>
        {
            if (await ApprovalFlow.ActAsync(db, current, Feature, d, true, request.Comment, cancellationToken) is { } problem) return problem;
            return d.Status == ApprovalStatus.Approved ? await PostAsync(db, current, d, cancellationToken) : null;
        }, cancellationToken);

    private static async Task<Results<Ok<TaxDepositDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db,
                d => ApprovalFlow.ActAsync(db, current, Feature, d, false, request.Comment, cancellationToken), cancellationToken);

    private static async Task<Results<Ok<TaxDepositDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db,
                d => ApprovalFlow.CancelAsync(db, current, Feature, d, request.Comment,
                    () => StockPosting.CancelAsync(db, current, Feature, d.Id, d.DepositDate, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var deposit = await db.TaxDeposits.SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (deposit is null) return TypedResults.NotFound();
        if (DocumentData.Delete(deposit) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<TaxDepositDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<TaxDeposit, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var deposit = await db.TaxDeposits.Include(d => d.Lines).SingleOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (deposit is null) return TypedResults.NotFound();
        if (await change(deposit) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<ProblemHttpResult?> PostAsync(
        ErpDbContext db, ICurrentContext current, TaxDeposit deposit, CancellationToken cancellationToken)
    {
        var key = deposit.Type == TaxType.Vds ? PostingKeys.VdsPayable : PostingKeys.TdsPayable;
        var (_, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            Feature, deposit.Id, deposit.Code, deposit.DepositDate, $"{deposit.Type.ToString().ToUpperInvariant()} deposited {deposit.Code}",
            [
                JournalEntryLine.Debit(key, deposit.TotalAmount),
                await PaymentSources.CreditAsync(db, deposit.PaymentMethodId, deposit.BankAccountId, deposit.TotalAmount, cancellationToken),
            ]), cancellationToken);
        return problem;
    }

    private static async Task<Results<Ok<TaxDepositDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        TaxDeposit deposit, SaveTaxDepositRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = deposit.Id > 0 ? ToRequest(deposit) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var type = Enum.Parse<TaxType>(request.Type, true);
        var code = await DocumentData.CodeAsync(db.TaxDeposits, deposit, request.Code, errors, cancellationToken);
        await PeriodLock.CheckAsync(db, "depositDate", request.DepositDate, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
            [("paymentMethodId", request.PaymentMethodId, existing?.PaymentMethodId, ProcurementLookups.PaymentMethod)], errors, cancellationToken);
        await PaymentSources.CheckAsync(db, request.PaymentMethodId, request.BankAccountId, existing?.BankAccountId, errors, cancellationToken);

        var ids = request.Lines.Select(l => l.PurchasePaymentId).Distinct().ToList();
        var taxes = (await TaxesAsync(db, type, ids, deposit.Id, cancellationToken)).ToDictionary(t => t.PurchasePaymentId);
        var problems = new List<string>();
        if (ids.Count != request.Lines.Count) problems.Add("Each payment can be added only once.");
        foreach (var line in request.Lines)
        {
            if (!taxes.TryGetValue(line.PurchasePaymentId, out var tax)) problems.Add("Every payment must be approved and have this tax.");
            else if (line.Amount > tax.Remaining) problems.Add($"Only {Math.Max(0, tax.Remaining):0.##} is left on {tax.PaymentCode}.");
        }

        if (problems.Count > 0) errors["lines"] = [.. problems.Distinct()];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        deposit.Lines.Clear();
        deposit.Lines.AddRange(request.Lines.Select((line, index) => new TaxDepositLine
        {
            Sequence = index + 1,
            PurchasePaymentId = line.PurchasePaymentId,
            Amount = line.Amount,
        }));
        deposit.TotalAmount = request.Lines.Sum(l => l.Amount);

        if (deposit.Id == 0) db.TaxDeposits.Add(deposit);
        deposit.Code = code ?? await DocumentData.NextCodeAsync(db.TaxDeposits, CodePrefix, cancellationToken);
        deposit.DepositDate = request.DepositDate;
        deposit.Type = type;
        deposit.PaymentMethodId = request.PaymentMethodId;
        deposit.BankAccountId = request.BankAccountId;
        deposit.ChallanNo = DocumentData.Trimmed(request.ChallanNo);
        deposit.ChallanDate = request.ChallanDate;
        deposit.Remarks = DocumentData.Trimmed(request.Remarks);
        deposit.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, deposit.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, deposit.Id, cancellationToken))!);
    }

    private static SaveTaxDepositRequest ToRequest(TaxDeposit d) => new()
    {
        Code = d.Code,
        DepositDate = d.DepositDate,
        Type = TypeKey(d.Type),
        PaymentMethodId = d.PaymentMethodId,
        BankAccountId = d.BankAccountId,
        ChallanNo = d.ChallanNo,
        ChallanDate = d.ChallanDate,
        Remarks = d.Remarks,
        Lines = [.. d.Lines.OrderBy(l => l.Sequence).Select(l => new SaveTaxDepositLine { PurchasePaymentId = l.PurchasePaymentId, Amount = l.Amount })],
        CustomData = CustomFieldData.Parse(d.CustomData),
    };

    private static async Task<TaxDepositDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var d = await db.TaxDeposits.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (d is null) return null;

        var ids = d.Lines.Select(l => l.PurchasePaymentId).ToList();
        var payments = await db.PurchasePayments.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.PaymentDate,
                SupplierName = db.Suppliers.Where(s => s.Id == p.SupplierId).Select(s => s.Name).FirstOrDefault(),
                Deducted = d.Type == TaxType.Vds ? p.Vds : p.Tds,
            })
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var taxes = (await TaxesAsync(db, d.Type, ids, d.Id, cancellationToken)).ToDictionary(t => t.PurchasePaymentId, t => t.Remaining);
        var names = await PartnerData.LookupNamesAsync(db, [d.PaymentMethodId], cancellationToken);

        return new TaxDepositDetail
        {
            Id = d.Id,
            Code = d.Code,
            DepositDate = d.DepositDate,
            Type = TypeKey(d.Type),
            PaymentMethodId = d.PaymentMethodId,
            PaymentMethodName = d.PaymentMethodId is { } method ? names.GetValueOrDefault(method) : null,
            BankAccountId = d.BankAccountId,
            BankAccountName = await db.BankAccounts.Where(b => b.Id == d.BankAccountId).Select(b => b.Name).FirstOrDefaultAsync(cancellationToken),
            ChallanNo = d.ChallanNo,
            ChallanDate = d.ChallanDate,
            Remarks = d.Remarks,
            Status = ApprovalFlow.StatusKey(d.Status),
            ReviewedAt = d.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, d.ReviewedById, cancellationToken),
            ReviewComment = d.ReviewComment,
            TotalAmount = d.TotalAmount,
            Lines = [.. d.Lines.OrderBy(l => l.Sequence).Select(l =>
            {
                var payment = payments[l.PurchasePaymentId];
                return new TaxDepositLineDetail(
                    l.Id, l.PurchasePaymentId, payment.Code, payment.PaymentDate, payment.SupplierName, payment.Deducted,
                    taxes.GetValueOrDefault(l.PurchasePaymentId), l.Amount);
            })],
            CustomData = CustomFieldData.Parse(d.CustomData),
            CreatedAt = d.CreatedAt,
            ModifiedAt = d.ModifiedAt,
        };
    }
}

internal sealed class TaxDepositApprovalSource : IApprovalSource
{
    public string Feature => AccountingMenu.TaxDeposit;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.TaxDeposits
            .Where(d => d.IsActive && (status == null || d.Status == status) && (ids == null || ids.Contains(d.Id)))
            .Select(d => new ApprovalDocumentInfo(
                d.Id,
                d.Code,
                d.DepositDate,
                d.ChallanNo,
                d.TotalAmount,
                d.Status,
                db.ErpUsers.Where(u => u.Id == d.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                d.CreatedAt));
}
