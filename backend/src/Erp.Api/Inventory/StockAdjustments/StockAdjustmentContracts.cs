using System.Text.Json;
using Erp.Api.Documents;
using Erp.Core.Domain.Inventory;
using FluentValidation;

namespace Erp.Api.Inventory.StockAdjustments;

public sealed record StockAdjustmentRow(
    int Id,
    string Code,
    DateOnly AdjustmentDate,
    string Type,
    Guid WarehouseId,
    string? WarehouseName,
    int? ReasonId,
    string? ReasonName,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    decimal TotalAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record StockAdjustmentList(IReadOnlyList<StockAdjustmentRow> Items, int Total, ApprovalCounts Counts);

public sealed record SaveStockAdjustmentLine
{
    public required int ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? Rate { get; init; }
    public required string? Remarks { get; init; }
}

public sealed record SaveStockAdjustmentRequest
{
    public required string? Code { get; init; }
    public required DateOnly AdjustmentDate { get; init; }
    public required string Type { get; init; }
    public required Guid WarehouseId { get; init; }
    public required int? ReasonId { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SaveStockAdjustmentLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record StockAdjustmentLineDetail(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    int UomId,
    string UomName,
    decimal Quantity,
    decimal Rate,
    decimal Amount,
    string? Remarks);

public sealed record StockAdjustmentDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly AdjustmentDate { get; init; }
    public required string Type { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required int? ReasonId { get; init; }
    public required string? ReasonName { get; init; }
    public required string? Remarks { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<StockAdjustmentLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveStockAdjustmentRequestValidator : AbstractValidator<SaveStockAdjustmentRequest>
{
    public SaveStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.Type).Must(t => Enum.TryParse<AdjustmentType>(t, true, out var type) && Enum.IsDefined(type))
            .WithMessage("Choose stock in, stock out or opening stock.");
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(DocumentData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Rate).GreaterThanOrEqualTo(0).When(l => l.Rate is not null);
            line.RuleFor(l => l.Remarks).MaximumLength(300);
        });
    }
}
