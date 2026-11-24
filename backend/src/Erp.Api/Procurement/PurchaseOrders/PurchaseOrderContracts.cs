using Erp.Api.Documents;
using System.Text.Json;
using FluentValidation;

namespace Erp.Api.Procurement.PurchaseOrders;

public sealed record PurchaseOrderRow(
    int Id,
    string Code,
    DateOnly OrderDate,
    int SupplierId,
    string? SupplierName,
    Guid WarehouseId,
    string? WarehouseName,
    DateOnly? ExpectedReceiveDate,
    string? RequisitionCode,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    decimal NetAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record PurchaseOrderList(IReadOnlyList<PurchaseOrderRow> Items, int Total, ApprovalCounts Counts);

public sealed record OrderRequisitionLine(
    int ItemId, string ItemCode, string ItemName, string UomName, decimal Quantity, decimal Rate, decimal VatPercentage, string? Remarks);

public sealed record OrderRequisitionOption(
    int Id, string Code, string Name, DateOnly RequestDate, Guid WarehouseId, bool IsActive, IReadOnlyList<OrderRequisitionLine> Lines);

public sealed record SavePurchaseOrderLine
{
    public required int ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? Price { get; init; }
    public required decimal? Discount { get; init; }
    public required decimal? VatPercentage { get; init; }
    public required string? Specification { get; init; }
}

public sealed record SavePurchaseOrderRequest
{
    public required string? Code { get; init; }
    public required DateOnly OrderDate { get; init; }
    public required int SupplierId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required int? RequisitionId { get; init; }
    public required string? ReferenceNo { get; init; }
    public required DateOnly? ExpectedReceiveDate { get; init; }
    public required int? PaymentTypeId { get; init; }
    public required int? PaymentDays { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required int? ItemTypeId { get; init; }
    public required string? ShippingAddress { get; init; }
    public required decimal? HeaderDiscount { get; init; }
    public required decimal? DeliveryCharge { get; init; }
    public required decimal? LaborCharge { get; init; }
    public required decimal? OtherCharge { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SavePurchaseOrderLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record PurchaseOrderLineDetail(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    int UomId,
    string UomName,
    decimal Quantity,
    decimal ReceivedQuantity,
    decimal Price,
    decimal TotalAmount,
    decimal Discount,
    decimal VatPercentage,
    decimal VatAmount,
    decimal NetAmount,
    string? Specification);

public sealed record PurchaseOrderDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly OrderDate { get; init; }
    public required int SupplierId { get; init; }
    public required string? SupplierName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required int? RequisitionId { get; init; }
    public required string? RequisitionName { get; init; }
    public required string? ReferenceNo { get; init; }
    public required DateOnly? ExpectedReceiveDate { get; init; }
    public required int? PaymentTypeId { get; init; }
    public required string? PaymentTypeName { get; init; }
    public required int? PaymentDays { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required int? ItemTypeId { get; init; }
    public required string? ItemTypeName { get; init; }
    public required string? ShippingAddress { get; init; }
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
    public required decimal ReceivedQuantity { get; init; }
    public required decimal ItemTotalAmount { get; init; }
    public required decimal TotalVatAmount { get; init; }
    public required decimal NetAmount { get; init; }
    public required IReadOnlyList<PurchaseOrderLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SavePurchaseOrderRequestValidator : AbstractValidator<SavePurchaseOrderRequest>
{
    public SavePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Supplier is required.");
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ExpectedReceiveDate).GreaterThanOrEqualTo(x => x.OrderDate).When(x => x.ExpectedReceiveDate is not null)
            .WithMessage("Use a date on or after the order date.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(DocumentData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Price).GreaterThanOrEqualTo(0).When(l => l.Price is not null);
            line.RuleFor(l => l.Discount).GreaterThanOrEqualTo(0).When(l => l.Discount is not null);
            line.RuleFor(l => l.VatPercentage).InclusiveBetween(0, 100).When(l => l.VatPercentage is not null);
            line.RuleFor(l => l.Specification).MaximumLength(1000);
        });
    }
}
