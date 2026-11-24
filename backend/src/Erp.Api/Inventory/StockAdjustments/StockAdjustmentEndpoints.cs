using Erp.Api.Accounting;
using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.PartnerManagement;
using Erp.Core.Authorization;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Inventory;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Inventory.StockAdjustments;

internal static class StockAdjustmentEndpoints
{
    private const string Feature = InventoryMenu.StockAdjustment;
    private const string CodePrefix = "ADJ-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var adjustments = group.MapGroup("/stock-adjustments");

        adjustments.MapGet("", List)
            .WithName("ListStockAdjustments")
            .WithSummary("Stock adjustments of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        adjustments.MapGet("/{id:int}", Get)
            .WithName("GetStockAdjustment")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        adjustments.MapPost("", Create)
            .WithName("CreateStockAdjustment")
            .WithSummary("Add to or take from the stock of a warehouse; an empty number gets the next ADJ- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveStockAdjustmentRequest>();

        adjustments.MapPut("/{id:int}", Update)
            .WithName("UpdateStockAdjustment")
            .WithSummary("Change a pending adjustment")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveStockAdjustmentRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        adjustments.MapGet("/{id:int}/approval", Approval)
            .WithName("GetStockAdjustmentApproval")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        adjustments.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveStockAdjustment")
            .WithSummary("The last approval posts the stock and the journal")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        adjustments.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectStockAdjustment")
            .WithSummary("Reject a pending adjustment; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        adjustments.MapDelete("/{id:int}", Delete)
            .WithName("DeleteStockAdjustment")
            .WithSummary("Delete a pending or rejected adjustment")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static string TypeKey(AdjustmentType type) => type.ToString().ToLowerInvariant();

    private static async Task<StockAdjustmentList> List(
        string? search,
        string? status,
        string? type,
        Guid? warehouseId,
        int? reasonId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var adjustments = db.StockAdjustments.AsNoTracking().Where(a => a.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            adjustments = adjustments.Where(a => a.Code.Contains(term) ||
                (a.Remarks != null && a.Remarks.Contains(term)) ||
                a.Lines.Any(l => l.Item!.Name.Contains(term) || l.Item.Code.Contains(term)));
        }

        if (Enum.TryParse<AdjustmentType>(type, true, out var only)) adjustments = adjustments.Where(a => a.Type == only);
        if (warehouseId is not null) adjustments = adjustments.Where(a => a.WarehouseId == warehouseId);
        if (reasonId is not null) adjustments = adjustments.Where(a => a.ReasonId == reasonId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(adjustments, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(a => a.Code),
            "-code" => filtered.OrderByDescending(a => a.Code),
            "adjustmentDate" => filtered.OrderBy(a => a.AdjustmentDate).ThenBy(a => a.Id),
            _ => filtered.OrderByDescending(a => a.AdjustmentDate).ThenByDescending(a => a.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(a => new
            {
                a.Id,
                a.Code,
                a.AdjustmentDate,
                a.Type,
                a.WarehouseId,
                WarehouseName = db.Warehouses.Where(w => w.Id == a.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                a.ReasonId,
                ReasonName = db.LookupValues.Where(v => v.Id == a.ReasonId).Select(v => v.NameEn).FirstOrDefault(),
                a.Status,
                ItemCount = a.Lines.Count,
                a.TotalQuantity,
                a.TotalAmount,
                a.CreatedAt,
                a.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new StockAdjustmentList(
            rows.Select(a => new StockAdjustmentRow(
                a.Id, a.Code, a.AdjustmentDate, TypeKey(a.Type), a.WarehouseId, a.WarehouseName, a.ReasonId, a.ReasonName,
                ApprovalFlow.StatusKey(a.Status), a.ItemCount, a.TotalQuantity, a.TotalAmount, a.CreatedAt, a.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<Results<Ok<StockAdjustmentDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<StockAdjustmentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveStockAdjustmentRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var adjustment = new StockAdjustment
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(adjustment, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<StockAdjustmentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveStockAdjustmentRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var adjustment = await db.StockAdjustments.Include(a => a.Lines).SingleOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        if (adjustment is null) return TypedResults.NotFound();
        if (adjustment.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(adjustment, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var adjustment = await db.StockAdjustments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        return adjustment is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, adjustment, cancellationToken));
    }

    private static Task<Results<Ok<StockAdjustmentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ReviewAsync(id, true, request, current, db, cancellationToken);

    private static async Task<Results<Ok<StockAdjustmentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing ? missing : await ReviewAsync(id, false, request, current, db, cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var adjustment = await db.StockAdjustments.SingleOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        if (adjustment is null) return TypedResults.NotFound();
        if (DocumentData.Delete(adjustment) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<StockAdjustmentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ReviewAsync(
        int id, bool approve, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var adjustment = await db.StockAdjustments.Include(a => a.Lines).SingleOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        if (adjustment is null) return TypedResults.NotFound();
        if (await ApprovalFlow.ActAsync(db, current, Feature, adjustment, approve, request.Comment, cancellationToken) is { } problem) return problem;
        if (adjustment.Status == ApprovalStatus.Approved && await PostAsync(db, current, adjustment, cancellationToken) is { } refused) return refused;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<ProblemHttpResult?> PostAsync(
        ErpDbContext db, ICurrentContext current, StockAdjustment adjustment, CancellationToken cancellationToken)
    {
        var lines = adjustment.Lines.OrderBy(l => l.Sequence).ToList();
        var (transaction, refused) = await StockPosting.PostAsync(db, current, new StockEntry(
            Feature, adjustment.Id, adjustment.Code, adjustment.AdjustmentDate, null, adjustment.Remarks,
            [.. lines.Select(l => adjustment.Type == AdjustmentType.Out
                ? StockEntryLine.Out(l.ItemId, l.UomId, adjustment.WarehouseId, l.Quantity)
                : StockEntryLine.In(l.ItemId, l.UomId, adjustment.WarehouseId, l.Quantity, l.Amount))]), cancellationToken);
        if (transaction is null) return refused;

        foreach (var (line, posted) in lines.Zip(transaction.Lines))
        {
            line.Amount = Math.Abs(posted.Value);
            line.Rate = Math.Round(line.Amount / line.Quantity, 4);
        }

        adjustment.TotalAmount = lines.Sum(l => l.Amount);
        var counterpart = adjustment.Type switch
        {
            AdjustmentType.In => PostingKeys.StockGain,
            AdjustmentType.Out => PostingKeys.StockLoss,
            _ => PostingKeys.OpeningBalance,
        };
        var value = transaction.Lines.Sum(l => l.Value);
        var (journal, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            Feature, adjustment.Id, adjustment.Code, adjustment.AdjustmentDate, $"Stock adjusted {adjustment.Code}",
            [JournalEntryLine.Debit(PostingKeys.Inventory, value), JournalEntryLine.Credit(counterpart, value)]), cancellationToken);
        transaction.Journal = journal;
        return problem;
    }

    private static async Task<Results<Ok<StockAdjustmentDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        StockAdjustment adjustment, SaveStockAdjustmentRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = adjustment.Id > 0 ? ToRequest(adjustment) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var type = Enum.Parse<AdjustmentType>(request.Type, true);
        var code = await DocumentData.CodeAsync(db.StockAdjustments, adjustment, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
            [("reasonId", request.ReasonId, existing?.ReasonId, StockLookups.AdjustmentReason)], errors, cancellationToken);
        await DocumentData.CheckWarehouseAsync(db, request.WarehouseId, existing?.WarehouseId, errors, cancellationToken);
        var items = await DocumentData.CheckItemsAsync(
            db, [.. request.Lines.Select(l => l.ItemId)], adjustment.Lines.Select(l => l.ItemId), errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var onHand = await StockData.OnHandAsync(db, request.WarehouseId, [.. items.Keys], cancellationToken);
        if (type == AdjustmentType.Out)
        {
            StockData.CheckEnough(request.Lines.Select(l => (l.ItemId, l.Quantity)), items, onHand, errors);
            if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
        }

        var lines = request.Lines.Select((line, index) =>
        {
            var rate = type == AdjustmentType.Out
                ? onHand.GetValueOrDefault(line.ItemId)?.Cost ?? 0
                : line.Rate ?? items[line.ItemId].Rate;
            return new StockAdjustmentLine
            {
                Sequence = index + 1,
                ItemId = line.ItemId,
                UomId = items[line.ItemId].UomId,
                Quantity = line.Quantity,
                Rate = rate,
                Amount = Math.Round(line.Quantity * rate, 2),
                Remarks = DocumentData.Trimmed(line.Remarks),
            };
        }).ToList();

        adjustment.Lines.Clear();
        adjustment.Lines.AddRange(lines);
        adjustment.TotalQuantity = lines.Sum(l => l.Quantity);
        adjustment.TotalAmount = lines.Sum(l => l.Amount);

        if (adjustment.Id == 0) db.StockAdjustments.Add(adjustment);
        adjustment.Code = code ?? await DocumentData.NextCodeAsync(db.StockAdjustments, CodePrefix, cancellationToken);
        adjustment.AdjustmentDate = request.AdjustmentDate;
        adjustment.Type = type;
        adjustment.WarehouseId = request.WarehouseId;
        adjustment.ReasonId = request.ReasonId;
        adjustment.Remarks = DocumentData.Trimmed(request.Remarks);
        adjustment.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, adjustment.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, adjustment.Id, cancellationToken))!);
    }

    private static SaveStockAdjustmentRequest ToRequest(StockAdjustment a) => new()
    {
        Code = a.Code,
        AdjustmentDate = a.AdjustmentDate,
        Type = TypeKey(a.Type),
        WarehouseId = a.WarehouseId,
        ReasonId = a.ReasonId,
        Remarks = a.Remarks,
        Lines = [.. a.Lines.OrderBy(l => l.Sequence).Select(l => new SaveStockAdjustmentLine
        {
            ItemId = l.ItemId,
            Quantity = l.Quantity,
            Rate = l.Rate,
            Remarks = l.Remarks,
        })],
        CustomData = CustomFieldData.Parse(a.CustomData),
    };

    private static async Task<StockAdjustmentDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var a = await db.StockAdjustments.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .Include(x => x.Lines).ThenInclude(l => l.Uom)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (a is null) return null;

        var names = await PartnerData.LookupNamesAsync(db, [a.ReasonId], cancellationToken);

        return new StockAdjustmentDetail
        {
            Id = a.Id,
            Code = a.Code,
            AdjustmentDate = a.AdjustmentDate,
            Type = TypeKey(a.Type),
            WarehouseId = a.WarehouseId,
            WarehouseName = await db.Warehouses.Where(w => w.Id == a.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            ReasonId = a.ReasonId,
            ReasonName = a.ReasonId is { } reason ? names.GetValueOrDefault(reason) : null,
            Remarks = a.Remarks,
            Status = ApprovalFlow.StatusKey(a.Status),
            ReviewedAt = a.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, a.ReviewedById, cancellationToken),
            ReviewComment = a.ReviewComment,
            TotalQuantity = a.TotalQuantity,
            TotalAmount = a.TotalAmount,
            Lines = [.. a.Lines.OrderBy(l => l.Sequence).Select(l => new StockAdjustmentLineDetail(
                l.Id, l.ItemId, l.Item!.Code, l.Item.Name, l.UomId, l.Uom!.Name, l.Quantity, l.Rate, l.Amount, l.Remarks))],
            CustomData = CustomFieldData.Parse(a.CustomData),
            CreatedAt = a.CreatedAt,
            ModifiedAt = a.ModifiedAt,
        };
    }
}
