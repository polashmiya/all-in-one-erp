using Erp.Api.Approvals;
using Erp.Api.Documents;
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

namespace Erp.Api.Accounting.Journals;

internal static class JournalEndpoints
{
    private const string Feature = AccountingMenu.Journal;
    private const string Manual = "manual";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var journals = group.MapGroup("/journals");

        journals.MapGet("", List)
            .WithName("ListJournals")
            .WithSummary("Journals of the selected business unit, automatic and manual, with counts by status")
            .RequirePermission($"{Feature}.view");

        journals.MapGet("/{id:int}", Get)
            .WithName("GetJournal")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        journals.MapPost("", Create)
            .WithName("CreateJournal")
            .WithSummary("Create a manual journal; an empty number gets the next JV- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveJournalRequest>();

        journals.MapPut("/{id:int}", Update)
            .WithName("UpdateJournal")
            .WithSummary("Change a pending manual journal")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveJournalRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        journals.MapGet("/{id:int}/approval", Approval)
            .WithName("GetJournalApproval")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        journals.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveJournal")
            .WithSummary("Only approved journals count in the ledgers")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        journals.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectJournal")
            .WithSummary("Reject a pending journal; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        journals.MapDelete("/{id:int}", Delete)
            .WithName("DeleteJournal")
            .WithSummary("Delete a pending or rejected manual journal")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<JournalList> List(
        string? search,
        string? status,
        string? source,
        DateOnly? from,
        DateOnly? to,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var journals = db.Journals.AsNoTracking().Where(j => j.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            journals = journals.Where(j => j.Code.Contains(term) || j.Narration.Contains(term) ||
                (j.DocumentCode != null && j.DocumentCode.Contains(term)));
        }

        if (source == Manual) journals = journals.Where(j => j.IsManual);
        else if (source is not null) journals = journals.Where(j => j.Feature == source);
        if (from is not null) journals = journals.Where(j => j.JournalDate >= from);
        if (to is not null) journals = journals.Where(j => j.JournalDate <= to);

        var (filtered, counts) = await DocumentData.ByStatusAsync(journals, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(j => j.Code),
            "-code" => filtered.OrderByDescending(j => j.Code),
            "journalDate" => filtered.OrderBy(j => j.JournalDate).ThenBy(j => j.Id),
            _ => filtered.OrderByDescending(j => j.JournalDate).ThenByDescending(j => j.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize).ToListAsync(cancellationToken);
        return new JournalList(
            [.. rows.Select(j => new JournalRow(
                j.Id, j.Code, j.JournalDate, j.Narration, j.Feature, j.DocumentId, j.DocumentCode, j.IsManual,
                ApprovalFlow.StatusKey(j.Status), j.TotalAmount, j.CreatedAt, j.ModifiedAt))],
            total,
            counts);
    }

    private static async Task<Results<Ok<JournalDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<JournalDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveJournalRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var journal = new Journal
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
            Narration = "",
            IsManual = true,
        };
        return SaveAsync(journal, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<JournalDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveJournalRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var journal = await ManualAsync(db, id, cancellationToken);
        if (journal is null) return TypedResults.NotFound();
        if (journal.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(journal, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var journal = await db.Journals.AsNoTracking().SingleOrDefaultAsync(j => j.Id == id && j.IsActive, cancellationToken);
        return journal is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, journal, cancellationToken));
    }

    private static Task<Results<Ok<JournalDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ReviewAsync(id, true, request, current, db, cancellationToken);

    private static async Task<Results<Ok<JournalDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing ? missing : await ReviewAsync(id, false, request, current, db, cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var journal = await ManualAsync(db, id, cancellationToken);
        if (journal is null) return TypedResults.NotFound();
        if (DocumentData.Delete(journal) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<Journal?> ManualAsync(ErpDbContext db, int id, CancellationToken cancellationToken) =>
        db.Journals.Include(j => j.Lines).SingleOrDefaultAsync(j => j.Id == id && j.IsActive && j.IsManual, cancellationToken);

    private static async Task<Results<Ok<JournalDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ReviewAsync(
        int id, bool approve, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var journal = await ManualAsync(db, id, cancellationToken);
        if (journal is null) return TypedResults.NotFound();
        if (await ApprovalFlow.ActAsync(db, current, Feature, journal, approve, request.Comment, cancellationToken) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<JournalDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        Journal journal, SaveJournalRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = journal.Id > 0 ? ToRequest(journal) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await DocumentData.CodeAsync(db.Journals, journal, request.Code, errors, cancellationToken);
        var problems = await LineProblemsAsync(db, request.Lines, cancellationToken);
        if (problems.Count > 0) errors["lines"] = [.. problems.Distinct()];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var lines = request.Lines.Select((line, index) => new JournalLine
        {
            Sequence = index + 1,
            ChartOfAccountId = line.ChartOfAccountId,
            SupplierId = line.SupplierId,
            Amount = (line.Debit ?? 0) - (line.Credit ?? 0),
            Remarks = DocumentData.Trimmed(line.Remarks),
        }).ToList();

        journal.Lines.Clear();
        journal.Lines.AddRange(lines);
        journal.TotalAmount = lines.Where(l => l.Amount > 0).Sum(l => l.Amount);

        if (journal.Id == 0) db.Journals.Add(journal);
        journal.Code = code ?? await DocumentData.NextCodeAsync(db.Journals, AccountPosting.CodePrefix, cancellationToken);
        journal.JournalDate = request.JournalDate;
        journal.Narration = request.Narration.Trim();
        journal.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, journal.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, journal.Id, cancellationToken))!);
    }

    private static async Task<List<string>> LineProblemsAsync(
        ErpDbContext db, IReadOnlyList<SaveJournalLine> lines, CancellationToken cancellationToken)
    {
        var ids = lines.Select(l => l.ChartOfAccountId).Distinct().ToList();
        var accounts = await db.ChartOfAccounts.AsNoTracking()
            .Where(a => ids.Contains(a.Id) && a.IsActive && !a.IsGroup)
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);
        var controls = await db.PostingAccounts.AsNoTracking()
            .Where(p => p.Key == PostingKeys.Inventory || p.Key == PostingKeys.AccountsPayable)
            .ToDictionaryAsync(p => p.Key, p => p.ChartOfAccountId, cancellationToken);
        var supplierIds = lines.Select(l => l.SupplierId).OfType<int>().Distinct().ToList();
        var suppliers = await db.Suppliers.Where(s => supplierIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(cancellationToken);

        var problems = new List<string>();
        foreach (var line in lines)
        {
            if (!accounts.TryGetValue(line.ChartOfAccountId, out var name))
            {
                problems.Add("Choose active accounts that are not groups.");
            }
            else if (controls.GetValueOrDefault(PostingKeys.Inventory) == line.ChartOfAccountId)
            {
                problems.Add($"{name} only changes with stock. Use a stock adjustment.");
            }
            else if (controls.GetValueOrDefault(PostingKeys.AccountsPayable) == line.ChartOfAccountId && line.SupplierId is null)
            {
                problems.Add($"Choose a supplier for {name}.");
            }

            if (line.SupplierId is { } supplierId && !suppliers.Contains(supplierId))
            {
                problems.Add("Choose suppliers of this business unit.");
            }
        }

        if (lines.Sum(l => l.Debit ?? 0) != lines.Sum(l => l.Credit ?? 0)) problems.Add("Total debit and total credit must be the same.");
        return problems;
    }

    private static SaveJournalRequest ToRequest(Journal j) => new()
    {
        Code = j.Code,
        JournalDate = j.JournalDate,
        Narration = j.Narration,
        Lines = [.. j.Lines.OrderBy(l => l.Sequence).Select(l => new SaveJournalLine
        {
            ChartOfAccountId = l.ChartOfAccountId,
            SupplierId = l.SupplierId,
            Debit = Math.Max(0, l.Amount),
            Credit = Math.Max(0, -l.Amount),
            Remarks = l.Remarks,
        })],
        CustomData = CustomFieldData.Parse(j.CustomData),
    };

    private static async Task<JournalDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var j = await db.Journals.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.ChartOfAccount)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (j is null) return null;

        var supplierIds = j.Lines.Select(l => l.SupplierId).OfType<int>().ToList();
        var suppliers = await db.Suppliers.AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return new JournalDetail
        {
            Id = j.Id,
            Code = j.Code,
            JournalDate = j.JournalDate,
            Narration = j.Narration,
            Feature = j.Feature,
            DocumentId = j.DocumentId,
            DocumentCode = j.DocumentCode,
            IsManual = j.IsManual,
            Status = ApprovalFlow.StatusKey(j.Status),
            ReviewedAt = j.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, j.ReviewedById, cancellationToken),
            ReviewComment = j.ReviewComment,
            TotalAmount = j.TotalAmount,
            Lines = [.. j.Lines.OrderBy(l => l.Sequence).Select(l => new JournalLineDetail(
                l.Id, l.ChartOfAccountId, l.ChartOfAccount!.Code, l.ChartOfAccount.Name, l.SupplierId,
                l.SupplierId is { } supplier ? suppliers.GetValueOrDefault(supplier) : null,
                Math.Max(0, l.Amount), Math.Max(0, -l.Amount), l.Remarks))],
            CustomData = CustomFieldData.Parse(j.CustomData),
            CreatedAt = j.CreatedAt,
            ModifiedAt = j.ModifiedAt,
        };
    }
}
