using Erp.Api.Documents;
using System.Text.Json;
using FluentValidation;

namespace Erp.Api.Procurement.PurchaseReceives;

public sealed record PurchaseReceiveRow(
    int Id,
    string Code,
    DateOnly ReceiveDate,
    int PurchaseOrderId,
    string? PurchaseOrderName,
    int SupplierId,
    string? SupplierName,
    Guid WarehouseId,
    string? WarehouseName,
    string? ChallanNo,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    decimal NetAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record PurchaseReceiveList(IReadOnlyList<PurchaseReceiveRow> Items, int Total, ApprovalCounts Counts);

public sealed record ReceiveOrderLine(
    int ItemId,
    string ItemCode,
    string ItemName,
    string UomName,
    decimal Ordered,
    decimal Received,
    decimal Remaining,
    decimal Price,
    decimal Discount,
    decimal VatPercentage);

public sealed record ReceiveOrderOption(
    int Id,
    string Code,
    string Name,
    int SupplierId,
    string? SupplierName,
    Guid WarehouseId,
    bool IsVatInclusive,
    int? PaymentTypeId,
    int? PaymentDays,
    bool IsActive,
    IReadOnlyList<ReceiveOrderLine> Lines);

public sealed record SavePurchaseReceiveLine
{
    public required int ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? Price { get; init; }
    public required decimal? Discount { get; init; }
    public required decimal? VatPercentage { get; init; }
    public required string? Batch { get; init; }
}

public sealed record SavePurchaseReceiveRequest
{
    public required string? Code { get; init; }
    public required DateOnly ReceiveDate { get; init; }
    public required int PurchaseOrderId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? ChallanNo { get; init; }
    public required DateOnly? ChallanDate { get; init; }
    public required string? VatChallanNo { get; init; }
    public required int? PaymentTypeId { get; init; }
    public required int? PaymentDays { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required decimal? HeaderDiscount { get; init; }
    public required decimal? DeliveryCharge { get; init; }
    public required decimal? LaborCharge { get; init; }
    public required decimal? OtherCharge { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SavePurchaseReceiveLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record PurchaseReceiveLineDetail(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    int UomId,
    string UomName,
    decimal Quantity,
    decimal ReturnedQuantity,
    decimal Price,
    decimal TotalAmount,
    decimal Discount,
    decimal VatPercentage,
    decimal VatAmount,
    decimal NetAmount,
    string? Batch);

public sealed record PurchaseReceiveDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly ReceiveDate { get; init; }
    public required int PurchaseOrderId { get; init; }
    public required string? PurchaseOrderName { get; init; }
    public required int SupplierId { get; init; }
    public required string? SupplierName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required string? ChallanNo { get; init; }
    public required DateOnly? ChallanDate { get; init; }
    public required string? VatChallanNo { get; init; }
    public required int? PaymentTypeId { get; init; }
    public required string? PaymentTypeName { get; init; }
    public required int? PaymentDays { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required decimal HeaderDiscount { get; init; }
    public required decimal DeliveryCharge { get; init; }
    public required decimal LaborCharge { get; init; }
    public required decimal OtherCharge { get; init; }
    public required string? Remarks { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal TotalLineDiscount { get; init; }
    public required decimal ItemTotalAmount { get; init; }
    public required decimal TotalVatAmount { get; init; }
    public required decimal NetAmount { get; init; }
    public required decimal ReturnAmount { get; init; }
    public required decimal PaidAmount { get; init; }
    public required IReadOnlyList<PurchaseReceiveLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SavePurchaseReceiveRequestValidator : AbstractValidator<SavePurchaseReceiveRequest>
{
    public SavePurchaseReceiveRequestValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0).WithMessage("Purchase order is required.");
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(DocumentData.MaxLines).OverridePropertyName("lines");
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
