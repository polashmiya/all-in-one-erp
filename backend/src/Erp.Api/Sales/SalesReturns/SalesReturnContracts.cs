using System.Text.Json;
using Erp.Api.Documents;
using FluentValidation;

namespace Erp.Api.Sales.SalesReturns;

public sealed record SalesReturnRow(
    int Id,
    string Code,
    string Source,
    DateOnly ReturnDate,
    int? DeliveryId,
    string? DeliveryName,
    int CustomerId,
    string CustomerName,
    Guid WarehouseId,
    string? WarehouseName,
    int? ReturnReasonId,
    string? ReturnReasonName,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    decimal NetAmount,
    decimal RefundAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record SalesReturnList(IReadOnlyList<SalesReturnRow> Items, int Total, ApprovalCounts Counts);

public sealed record ReturnDeliveryLine(
    int DeliveryLineId, int ItemId, string ItemCode, string ItemName, string UomName, string? ItemDescription, decimal Delivered,
    decimal Remaining, decimal Price, decimal DiscountPerUnit, decimal SdPercentage, decimal VatPercentage, decimal AitPercentage,
    bool IsOfferItem, string? Batch, string? ItemSerial);

public sealed record ReturnDeliveryOption
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required DateOnly DeliveryDate { get; init; }
    public required string? OrderCode { get; init; }
    public required int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string? Phone { get; init; }
    public required Guid? OfficeId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required int? SalesForceId { get; init; }
    public required string? ChallanNo { get; init; }
    public required string? PickupAddress { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required decimal Due { get; init; }
    public required decimal ReductionRate { get; init; }
    public required decimal ChargeRefundable { get; init; }
    public required IReadOnlyList<ReturnDeliveryLine> Lines { get; init; }
}

public sealed record SaveSalesReturnLine : SaveSalesLine
{
    public required int? DeliveryLineId { get; init; }
    public required decimal? CostPrice { get; init; }
    public required bool IsDamaged { get; init; }
    public required string? Batch { get; init; }
    public required string? ItemSerial { get; init; }
    public required string? Remarks { get; init; }
}

public sealed record SaveSalesReturnRequest
{
    public required string? Code { get; init; }
    public required string Source { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required int? DeliveryId { get; init; }
    public required int CustomerId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required Guid? OfficeId { get; init; }
    public required int? SalesForceId { get; init; }
    public required string? Phone { get; init; }
    public required int? ReturnTypeId { get; init; }
    public required int? ReturnReasonId { get; init; }
    public required int? ReceivedById { get; init; }
    public required string? ReturnRequestNo { get; init; }
    public required string? ReturnRefNo { get; init; }
    public required DateOnly? ReturnRefDate { get; init; }
    public required decimal? ReturnRefAmount { get; init; }
    public required string? CreditNoteNo { get; init; }
    public required DateOnly? CreditNoteDate { get; init; }
    public required string? ChallanNo { get; init; }
    public required string? VatChallanNo { get; init; }
    public required int? DeliveryMethodId { get; init; }
    public required string? PickupAddress { get; init; }
    public required bool IsExchange { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required decimal? ChargeRefundAmount { get; init; }
    public required string? DeductionLabel { get; init; }
    public required decimal? DeductionValue { get; init; }
    public required decimal? RefundAmount { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required string? Remarks { get; init; }
    public required string? Narration { get; init; }
    public required IReadOnlyList<SaveSalesReturnLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record SalesReturnLineDetail(
    int Id,
    int? DeliveryLineId,
    int ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    string UomName,
    decimal? Delivered,
    decimal? Remaining,
    decimal Quantity,
    decimal Price,
    decimal TotalAmount,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    bool IsAmountWiseDiscount,
    bool IsDiscountPerQuantity,
    decimal SdPercentage,
    decimal SdAmount,
    decimal VatPercentage,
    decimal VatAmount,
    decimal AitPercentage,
    decimal AitAmount,
    decimal TotalValue,
    bool IsOfferItem,
    bool IsDamaged,
    decimal CostPrice,
    decimal CostTotal,
    decimal LossAmount,
    string? Batch,
    string? ItemSerial,
    string? Remarks);

public sealed record SalesReturnDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Source { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required int? DeliveryId { get; init; }
    public required string? DeliveryName { get; init; }
    public required int? OrderId { get; init; }
    public required string? OrderName { get; init; }
    public required int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string? Phone { get; init; }
    public required Guid? OfficeId { get; init; }
    public required string? OfficeName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required int? SalesForceId { get; init; }
    public required string? SalesForceName { get; init; }
    public required int? ReturnTypeId { get; init; }
    public required string? ReturnTypeName { get; init; }
    public required int? ReturnReasonId { get; init; }
    public required string? ReturnReasonName { get; init; }
    public required int? ReceivedById { get; init; }
    public required string? ReceivedByName { get; init; }
    public required string? ReturnRequestNo { get; init; }
    public required string? ReturnRefNo { get; init; }
    public required DateOnly? ReturnRefDate { get; init; }
    public required decimal ReturnRefAmount { get; init; }
    public required string? CreditNoteNo { get; init; }
    public required DateOnly? CreditNoteDate { get; init; }
    public required string? ChallanNo { get; init; }
    public required string? VatChallanNo { get; init; }
    public required int? DeliveryMethodId { get; init; }
    public required string? DeliveryMethodName { get; init; }
    public required string? PickupAddress { get; init; }
    public required bool IsExchange { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal ItemTotalAmount { get; init; }
    public required decimal TotalDiscount { get; init; }
    public required decimal TotalSd { get; init; }
    public required decimal TotalVat { get; init; }
    public required decimal TotalAit { get; init; }
    public required decimal SpecialDiscountAmount { get; init; }
    public required decimal ChargeRefundAmount { get; init; }
    public required string? DeductionLabel { get; init; }
    public required decimal DeductionValue { get; init; }
    public required decimal NetAmount { get; init; }
    public required decimal DueAdjusted { get; init; }
    public required decimal RefundAmount { get; init; }
    public required decimal AdvanceAmount { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required string? PaymentMethodName { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? BankAccountName { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required decimal TotalCost { get; init; }
    public required decimal LossAmount { get; init; }
    public required string? Remarks { get; init; }
    public required string? Narration { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal Due { get; init; }
    public required decimal ReductionRate { get; init; }
    public required decimal ChargeRefundable { get; init; }
    public required IReadOnlyList<SalesReturnLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveSalesReturnRequestValidator : AbstractValidator<SaveSalesReturnRequest>
{
    public SaveSalesReturnRequestValidator()
    {
        RuleFor(x => x.Source).Must(s => s is SalesReturnEndpoints.FromDelivery or SalesReturnEndpoints.WithoutReference)
            .WithMessage("Choose where the return comes from.");
        RuleFor(x => x.DeliveryId).NotNull().When(x => x.Source == SalesReturnEndpoints.FromDelivery).WithMessage("Choose the delivery.");
        RuleFor(x => x.DeliveryId).Null().When(x => x.Source == SalesReturnEndpoints.WithoutReference)
            .WithMessage("A return without reference has no delivery.");
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines.Count).InclusiveBetween(1, DocumentData.MaxLines).OverridePropertyName("lines")
            .WithMessage("Add at least one item.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            SalesValidation.AddLineRules(line);
            line.RuleFor(l => l.CostPrice).GreaterThanOrEqualTo(0).When(l => l.CostPrice is not null);
            line.RuleFor(l => l.Batch).MaximumLength(500);
            line.RuleFor(l => l.ItemSerial).MaximumLength(2000);
            line.RuleFor(l => l.Remarks).MaximumLength(500);
        });
    }
}
