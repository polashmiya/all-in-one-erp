using System.Text.Json;
using Erp.Api.Documents;
using FluentValidation;

namespace Erp.Api.Inventory.StockTransfers;

public sealed record StockTransferRow(
    int Id,
    string Code,
    DateOnly TransferDate,
    Guid? FromOfficeId,
    string? FromOfficeName,
    Guid FromWarehouseId,
    string? FromWarehouseName,
    Guid? ToOfficeId,
    string? ToOfficeName,
    Guid ToWarehouseId,
    string? ToWarehouseName,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record StockTransferList(IReadOnlyList<StockTransferRow> Items, int Total, ApprovalCounts Counts);

public sealed record SaveStockTransferLine
{
    public required int ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required string? Remarks { get; init; }
}

public sealed record SaveStockTransferRequest
{
    public required string? Code { get; init; }
    public required DateOnly TransferDate { get; init; }
    public required Guid? FromOfficeId { get; init; }
    public required Guid FromWarehouseId { get; init; }
    public required Guid? ToOfficeId { get; init; }
    public required Guid ToWarehouseId { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SaveStockTransferLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record StockTransferLineDetail(
    int Id, int ItemId, string ItemCode, string ItemName, int UomId, string UomName, decimal Quantity, string? Remarks);

public sealed record StockTransferDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly TransferDate { get; init; }
    public required Guid? FromOfficeId { get; init; }
    public required Guid FromWarehouseId { get; init; }
    public required string? FromOfficeName { get; init; }
    public required string? FromWarehouseName { get; init; }
    public required Guid? ToOfficeId { get; init; }
    public required Guid ToWarehouseId { get; init; }
    public required string? ToOfficeName { get; init; }
    public required string? ToWarehouseName { get; init; }
    public required string? Remarks { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required IReadOnlyList<StockTransferLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveStockTransferRequestValidator : AbstractValidator<SaveStockTransferRequest>
{
    public SaveStockTransferRequestValidator()
    {
        RuleFor(x => x.FromWarehouseId).NotEmpty();
        RuleFor(x => x.ToWarehouseId).NotEmpty();
        RuleFor(x => x.ToWarehouseId).NotEqual(x => x.FromWarehouseId).WithMessage("Choose another warehouse.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(DocumentData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Remarks).MaximumLength(300);
        });
    }
}
