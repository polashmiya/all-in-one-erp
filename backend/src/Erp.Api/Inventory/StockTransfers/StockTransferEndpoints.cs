using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Core.Authorization;
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

namespace Erp.Api.Inventory.StockTransfers;

internal static class StockTransferEndpoints
{
    private const string Feature = InventoryMenu.StockTransfer;
    private const string CodePrefix = "TRF-";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var transfers = group.MapGroup("/stock-transfers");

        transfers.MapGet("", List)
            .WithName("ListStockTransfers")
            .WithSummary("Stock moved between warehouses of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        transfers.MapGet("/{id:int}", Get)
            .WithName("GetStockTransfer")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        transfers.MapPost("", Create)
            .WithName("CreateStockTransfer")
            .WithSummary("Move stock to another warehouse; an empty number gets the next TRF- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveStockTransferRequest>();

        transfers.MapPut("/{id:int}", Update)
            .WithName("UpdateStockTransfer")
            .WithSummary("Change a pending transfer")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveStockTransferRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        transfers.MapGet("/{id:int}/approval", Approval)
            .WithName("GetStockTransferApproval")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        transfers.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveStockTransfer")
            .WithSummary("The last approval moves the stock")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        transfers.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectStockTransfer")
            .WithSummary("Reject a pending transfer; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        transfers.MapDelete("/{id:int}", Delete)
            .WithName("DeleteStockTransfer")
            .WithSummary("Delete a pending or rejected transfer")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<StockTransferList> List(
        string? search,
        string? status,
        Guid? fromWarehouseId,
        Guid? toWarehouseId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var transfers = db.StockTransfers.AsNoTracking().Where(t => t.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            transfers = transfers.Where(t => t.Code.Contains(term) ||
                (t.Remarks != null && t.Remarks.Contains(term)) ||
                t.Lines.Any(l => l.Item!.Name.Contains(term) || l.Item.Code.Contains(term)));
        }

        if (fromWarehouseId is not null) transfers = transfers.Where(t => t.FromWarehouseId == fromWarehouseId);
        if (toWarehouseId is not null) transfers = transfers.Where(t => t.ToWarehouseId == toWarehouseId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(transfers, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(t => t.Code),
            "-code" => filtered.OrderByDescending(t => t.Code),
            "transferDate" => filtered.OrderBy(t => t.TransferDate).ThenBy(t => t.Id),
            _ => filtered.OrderByDescending(t => t.TransferDate).ThenByDescending(t => t.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(t => new
            {
                t.Id,
                t.Code,
                t.TransferDate,
                t.FromWarehouseId,
                FromWarehouseName = db.Warehouses.Where(w => w.Id == t.FromWarehouseId).Select(w => w.Name).FirstOrDefault(),
                t.ToWarehouseId,
                ToWarehouseName = db.Warehouses.Where(w => w.Id == t.ToWarehouseId).Select(w => w.Name).FirstOrDefault(),
                t.Status,
                ItemCount = t.Lines.Count,
                t.TotalQuantity,
                t.CreatedAt,
                t.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new StockTransferList(
            rows.Select(t => new StockTransferRow(
                t.Id, t.Code, t.TransferDate, t.FromWarehouseId, t.FromWarehouseName, t.ToWarehouseId, t.ToWarehouseName,
                ApprovalFlow.StatusKey(t.Status), t.ItemCount, t.TotalQuantity, t.CreatedAt, t.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<Results<Ok<StockTransferDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<StockTransferDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveStockTransferRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var transfer = new StockTransfer
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(transfer, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<StockTransferDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveStockTransferRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var transfer = await db.StockTransfers.Include(t => t.Lines).SingleOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);
        if (transfer is null) return TypedResults.NotFound();
        if (transfer.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(transfer, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var transfer = await db.StockTransfers.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);
        return transfer is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, transfer, cancellationToken));
    }

    private static Task<Results<Ok<StockTransferDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ReviewAsync(id, true, request, current, db, cancellationToken);

    private static async Task<Results<Ok<StockTransferDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing ? missing : await ReviewAsync(id, false, request, current, db, cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var transfer = await db.StockTransfers.SingleOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);
        if (transfer is null) return TypedResults.NotFound();
        if (DocumentData.Delete(transfer) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<StockTransferDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ReviewAsync(
        int id, bool approve, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var transfer = await db.StockTransfers.Include(t => t.Lines).SingleOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);
        if (transfer is null) return TypedResults.NotFound();
        if (await ApprovalFlow.ActAsync(db, current, Feature, transfer, approve, request.Comment, cancellationToken) is { } problem) return problem;
        if (transfer.Status == ApprovalStatus.Approved)
        {
            var (_, refused) = await StockPosting.PostAsync(db, current, new StockEntry(
                Feature, transfer.Id, transfer.Code, transfer.TransferDate, null, transfer.Remarks,
                [.. transfer.Lines.OrderBy(l => l.Sequence).Select(l =>
                    StockEntryLine.Move(l.ItemId, l.UomId, transfer.FromWarehouseId, transfer.ToWarehouseId, l.Quantity))]), cancellationToken);
            if (refused is not null) return refused;
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<StockTransferDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        StockTransfer transfer, SaveStockTransferRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = transfer.Id > 0 ? ToRequest(transfer) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await DocumentData.CodeAsync(db.StockTransfers, transfer, request.Code, errors, cancellationToken);
        await DocumentData.CheckWarehouseAsync(db, request.FromWarehouseId, existing?.FromWarehouseId, errors, cancellationToken, "fromWarehouseId");
        await DocumentData.CheckWarehouseAsync(db, request.ToWarehouseId, existing?.ToWarehouseId, errors, cancellationToken, "toWarehouseId");
        var items = await DocumentData.CheckItemsAsync(
            db, [.. request.Lines.Select(l => l.ItemId)], transfer.Lines.Select(l => l.ItemId), errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var onHand = await StockData.OnHandAsync(db, request.FromWarehouseId, [.. items.Keys], cancellationToken);
        StockData.CheckEnough(request.Lines.Select(l => (l.ItemId, l.Quantity)), items, onHand, errors);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var lines = request.Lines.Select((line, index) => new StockTransferLine
        {
            Sequence = index + 1,
            ItemId = line.ItemId,
            UomId = items[line.ItemId].UomId,
            Quantity = line.Quantity,
            Remarks = DocumentData.Trimmed(line.Remarks),
        }).ToList();

        transfer.Lines.Clear();
        transfer.Lines.AddRange(lines);
        transfer.TotalQuantity = lines.Sum(l => l.Quantity);

        if (transfer.Id == 0) db.StockTransfers.Add(transfer);
        transfer.Code = code ?? await DocumentData.NextCodeAsync(db.StockTransfers, CodePrefix, cancellationToken);
        transfer.TransferDate = request.TransferDate;
        transfer.FromWarehouseId = request.FromWarehouseId;
        transfer.ToWarehouseId = request.ToWarehouseId;
        transfer.Remarks = DocumentData.Trimmed(request.Remarks);
        transfer.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, transfer.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, transfer.Id, cancellationToken))!);
    }

    private static SaveStockTransferRequest ToRequest(StockTransfer t) => new()
    {
        Code = t.Code,
        TransferDate = t.TransferDate,
        FromWarehouseId = t.FromWarehouseId,
        ToWarehouseId = t.ToWarehouseId,
        Remarks = t.Remarks,
        Lines = [.. t.Lines.OrderBy(l => l.Sequence).Select(l => new SaveStockTransferLine
        {
            ItemId = l.ItemId,
            Quantity = l.Quantity,
            Remarks = l.Remarks,
        })],
        CustomData = CustomFieldData.Parse(t.CustomData),
    };

    private static async Task<StockTransferDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var t = await db.StockTransfers.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .Include(x => x.Lines).ThenInclude(l => l.Uom)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (t is null) return null;

        var warehouses = await db.Warehouses.AsNoTracking()
            .Where(w => w.Id == t.FromWarehouseId || w.Id == t.ToWarehouseId)
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        return new StockTransferDetail
        {
            Id = t.Id,
            Code = t.Code,
            TransferDate = t.TransferDate,
            FromWarehouseId = t.FromWarehouseId,
            FromWarehouseName = warehouses.GetValueOrDefault(t.FromWarehouseId),
            ToWarehouseId = t.ToWarehouseId,
            ToWarehouseName = warehouses.GetValueOrDefault(t.ToWarehouseId),
            Remarks = t.Remarks,
            Status = ApprovalFlow.StatusKey(t.Status),
            ReviewedAt = t.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, t.ReviewedById, cancellationToken),
            ReviewComment = t.ReviewComment,
            TotalQuantity = t.TotalQuantity,
            Lines = [.. t.Lines.OrderBy(l => l.Sequence).Select(l => new StockTransferLineDetail(
                l.Id, l.ItemId, l.Item!.Code, l.Item.Name, l.UomId, l.Uom!.Name, l.Quantity, l.Remarks))],
            CustomData = CustomFieldData.Parse(t.CustomData),
            CreatedAt = t.CreatedAt,
            ModifiedAt = t.ModifiedAt,
        };
    }
}
