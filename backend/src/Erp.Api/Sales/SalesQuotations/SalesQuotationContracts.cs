using System.Text.Json;
using Erp.Api.Documents;
using FluentValidation;

namespace Erp.Api.Sales.SalesQuotations;

public sealed record SalesQuotationRow(
    int Id,
    string Code,
    string Version,
    DateOnly QuotationDate,
    DateOnly ValidityDate,
    bool IsExpired,
    int CustomerId,
    string CustomerName,
    int? QuotationTypeId,
    string? QuotationTypeName,
    Guid? OfficeId,
    string? OfficeName,
    int? SalesForceId,
    string? SalesForceName,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    decimal TotalAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record SalesQuotationList(IReadOnlyList<SalesQuotationRow> Items, int Total, ApprovalCounts Counts);

public sealed record SaveSalesQuotationLine
{
    public required int ItemId { get; init; }
    public required string? ItemDescription { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? Price { get; init; }
    public required decimal? DiscountPercentage { get; init; }
    public required decimal? DiscountAmount { get; init; }
    public required bool IsAmountWiseDiscount { get; init; }
    public required bool IsDiscountPerQuantity { get; init; }
    public required decimal? SdPercentage { get; init; }
    public required decimal? VatPercentage { get; init; }
    public required decimal? AitPercentage { get; init; }
    public required int? WarrantyInMonth { get; init; }
    public required string? DeliveryDate { get; init; }
}

public sealed record SaveSalesQuotationRequest
{
    public required string? Code { get; init; }
    public required int? QuotationTypeId { get; init; }
    public required string? Version { get; init; }
    public required DateOnly? VersionDate { get; init; }
    public required DateOnly QuotationDate { get; init; }
    public required DateOnly ValidityDate { get; init; }
    public required int CustomerId { get; init; }
    public required Guid? OfficeId { get; init; }
    public required int? SalesForceId { get; init; }
    public required string? Reference { get; init; }
    public required string? BillingContactPerson { get; init; }
    public required string? BillingPhoneNo { get; init; }
    public required string? BillingAddress { get; init; }
    public required string? ShippingContactPerson { get; init; }
    public required int? ShippingAddressId { get; init; }
    public required string? ShippingAddressName { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required bool IsGrossDiscount { get; init; }
    public required decimal? DiscountPercentage { get; init; }
    public required decimal? DiscountAmount { get; init; }
    public required decimal? DeliveryCharge { get; init; }
    public required decimal? ExtraCharge { get; init; }
    public required string? Subjects { get; init; }
    public required string? BodyNarration { get; init; }
    public required string? FooterNarration { get; init; }
    public required string? Narration { get; init; }
    public required IReadOnlyList<SaveSalesQuotationLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record SalesQuotationLineDetail(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    int UomId,
    string UomName,
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
    int WarrantyInMonth,
    string? DeliveryDate,
    decimal ApproveQuantity,
    decimal ApprovePrice,
    decimal ApproveAmount);

public sealed record SalesQuotationVersion(int Id, string Version, DateOnly? VersionDate, string Status, decimal TotalAmount);

public sealed record SalesQuotationDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required int? QuotationTypeId { get; init; }
    public required string? QuotationTypeName { get; init; }
    public required string Version { get; init; }
    public required DateOnly? VersionDate { get; init; }
    public required DateOnly QuotationDate { get; init; }
    public required DateOnly ValidityDate { get; init; }
    public required bool IsExpired { get; init; }
    public required int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid? OfficeId { get; init; }
    public required string? OfficeName { get; init; }
    public required int? SalesForceId { get; init; }
    public required string? SalesForceName { get; init; }
    public required int? ReferenceId { get; init; }
    public required string? Reference { get; init; }
    public required string? BillingContactPerson { get; init; }
    public required string? BillingPhoneNo { get; init; }
    public required string? BillingAddress { get; init; }
    public required string? ShippingContactPerson { get; init; }
    public required int? ShippingAddressId { get; init; }
    public required string? ShippingAddressName { get; init; }
    public required string? ShipToName { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required bool IsGrossDiscount { get; init; }
    public required decimal DiscountPercentage { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required decimal DeliveryCharge { get; init; }
    public required decimal ExtraCharge { get; init; }
    public required string? Subjects { get; init; }
    public required string? BodyNarration { get; init; }
    public required string? FooterNarration { get; init; }
    public required string? Narration { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required bool IsOrdered { get; init; }
    public required bool CanRevise { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal GrossAmount { get; init; }
    public required decimal SdAmount { get; init; }
    public required decimal VatAmount { get; init; }
    public required decimal AitAmount { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<SalesQuotationLineDetail> Lines { get; init; }
    public required IReadOnlyList<SalesQuotationVersion> Versions { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveSalesQuotationRequestValidator : AbstractValidator<SaveSalesQuotationRequest>
{
    public SaveSalesQuotationRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
        RuleFor(x => x.ValidityDate).GreaterThanOrEqualTo(x => x.QuotationDate)
            .WithMessage("Use a date on or after the quotation date.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(DocumentData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.ItemDescription).MaximumLength(2000);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Price).GreaterThanOrEqualTo(0).When(l => l.Price is not null);
            line.RuleFor(l => l.DiscountPercentage).InclusiveBetween(0, 100).When(l => l.DiscountPercentage is not null);
            line.RuleFor(l => l.DiscountAmount).GreaterThanOrEqualTo(0).When(l => l.DiscountAmount is not null);
            line.RuleFor(l => l.SdPercentage).InclusiveBetween(0, 1000).When(l => l.SdPercentage is not null);
            line.RuleFor(l => l.VatPercentage).InclusiveBetween(0, 100).When(l => l.VatPercentage is not null);
            line.RuleFor(l => l.AitPercentage).InclusiveBetween(0, 100).When(l => l.AitPercentage is not null);
            line.RuleFor(l => l.WarrantyInMonth).InclusiveBetween(0, 1200).When(l => l.WarrantyInMonth is not null);
            line.RuleFor(l => l.DeliveryDate).MaximumLength(300);
        });
    }
}
