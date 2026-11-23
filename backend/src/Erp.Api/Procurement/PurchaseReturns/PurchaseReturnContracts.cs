using System.Text.Json;
using FluentValidation;

namespace Erp.Api.Procurement.PurchaseReturns;

public sealed record PurchaseReturnRow(
    int Id,
    string Code,
    DateOnly ReturnDate,
    int PurchaseReceiveId,
    string? PurchaseReceiveName,
    int SupplierId,
    string? SupplierName,
    Guid WarehouseId,
    string? WarehouseName,
    int? ReasonId,
    string? ReasonName,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    decimal NetAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record PurchaseReturnList(IReadOnlyList<PurchaseReturnRow> Items, int Total, ApprovalCounts Counts);

public sealed record ReturnReceiveLine(
    int ItemId,
    string ItemCode,
    string ItemName,
    string UomName,
    decimal Received,
    decimal Returned,
    decimal Remaining,
    decimal Price,
    decimal Discount,
    decimal VatPercentage,
    string? Batch);

public sealed record ReturnReceiveOption(
    int Id,
    string Code,
    string Name,
    int SupplierId,
    string? SupplierName,
    Guid WarehouseId,
    bool IsVatInclusive,
    bool IsActive,
    IReadOnlyList<ReturnReceiveLine> Lines);

public sealed record SavePurchaseReturnLine
{
    public required int ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? Price { get; init; }
    public required decimal? Discount { get; init; }
    public required decimal? VatPercentage { get; init; }
    public required string? Batch { get; init; }
}

public sealed record SavePurchaseReturnRequest
{
    public required string? Code { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required int PurchaseReceiveId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required int? ReasonId { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SavePurchaseReturnLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record PurchaseReturnLineDetail(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    int UomId,
    string UomName,
    decimal Quantity,
    decimal Price,
    decimal TotalAmount,
    decimal Discount,
    decimal VatPercentage,
    decimal VatAmount,
    decimal NetAmount,
    string? Batch);

public sealed record PurchaseReturnDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required int PurchaseReceiveId { get; init; }
    public required string? PurchaseReceiveName { get; init; }
    public required int PurchaseOrderId { get; init; }
    public required string? PurchaseOrderName { get; init; }
    public required int SupplierId { get; init; }
    public required string? SupplierName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required int? ReasonId { get; init; }
    public required string? ReasonName { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required string? Remarks { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal ItemTotalAmount { get; init; }
    public required decimal TotalVatAmount { get; init; }
    public required decimal NetAmount { get; init; }
    public required IReadOnlyList<PurchaseReturnLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SavePurchaseReturnRequestValidator : AbstractValidator<SavePurchaseReturnRequest>
{
    public SavePurchaseReturnRequestValidator()
    {
        RuleFor(x => x.PurchaseReceiveId).GreaterThan(0).WithMessage("Goods receive is required.");
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(ProcurementData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Price).GreaterThanOrEqualTo(0).When(l => l.Price is not null);
            line.RuleFor(l => l.Discount).GreaterThanOrEqualTo(0).When(l => l.Discount is not null);
            line.RuleFor(l => l.VatPercentage).InclusiveBetween(0, 100).When(l => l.VatPercentage is not null);
            line.RuleFor(l => l.Batch).MaximumLength(100);
        });
    }
}
