using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Erp.Api.Documents;
using Erp.Core.Domain.Sales;
using FluentValidation;

namespace Erp.Api.Sales.SalesOrders;

public sealed record SalesOrderRow(
    int Id,
    string Code,
    DateOnly OrderDate,
    DateOnly DeliveryDate,
    int CustomerId,
    string CustomerName,
    int? SalesQuotationId,
    string? SalesQuotationName,
    Guid WarehouseId,
    string? WarehouseName,
    int? SalesForceId,
    string? SalesForceName,
    string Status,
    bool IsCompleted,
    int ItemCount,
    decimal TotalQuantity,
    decimal DeliveredQuantity,
    decimal NetAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record SalesOrderList(IReadOnlyList<SalesOrderRow> Items, int Total, ApprovalCounts Counts);

public sealed record OrderQuotationLine(
    int ItemId, string? ItemCode, string ItemName, string UomName, string? ItemDescription, decimal Quantity, decimal Price,
    decimal DiscountPercentage, decimal DiscountAmount, bool IsAmountWiseDiscount, bool IsDiscountPerQuantity, decimal SdPercentage,
    decimal VatPercentage, decimal AitPercentage, int WarrantyInMonth);

public sealed record OrderQuotationOption
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid? OfficeId { get; init; }
    public required int? SalesForceId { get; init; }
    public required string? ShippingContactPerson { get; init; }
    public required int? ShippingAddressId { get; init; }
    public required string? ShippingAddressName { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required bool IsGrossDiscount { get; init; }
    public required decimal HeaderDiscountPercentage { get; init; }
    public required decimal HeaderDiscount { get; init; }
    public required decimal OthersCost { get; init; }
    public required decimal ExtraCharge { get; init; }
    public required IReadOnlyList<OrderQuotationLine> Lines { get; init; }
}

public sealed record SaveSalesOrderLine : SaveSalesLine
{
    public required decimal? DemandQuantity { get; init; }
    public required decimal? FobRate { get; init; }
    public required decimal? FreightRate { get; init; }
}

public sealed record SaveSalesOrderRequest : SaveSalesDocumentRequest
{
    [JsonConstructor]
    public SaveSalesOrderRequest()
    {
    }

    [SetsRequiredMembers]
    public SaveSalesOrderRequest(SalesOrder o)
        : base(o)
    {
        SalesQuotationId = o.SalesQuotationId;
        AdvanceAmount = o.AdvanceAmount;
        DueLimit = o.DueLimit;
        CustomerPoAmount = o.CustomerPoAmount;
        CustomerPoRefAmount = o.CustomerPoRefAmount;
        SalesTermId = o.SalesTermId;
        DmsInvoice = o.DmsInvoice;
        IsForeign = o.IsForeign;
        LcTypeId = o.LcTypeId;
        LcNo = o.LcNo;
        PaymentTermsId = o.PaymentTermsId;
        IsAllowPartialShipment = o.IsAllowPartialShipment;
        IsAllowTransitShipment = o.IsAllowTransitShipment;
        Review = o.Review;
        Lines = [];
    }

    public required int? SalesQuotationId { get; init; }
    public required decimal? AdvanceAmount { get; init; }
    public required decimal? DueLimit { get; init; }
    public required decimal? CustomerPoAmount { get; init; }
    public required decimal? CustomerPoRefAmount { get; init; }
    public required int? SalesTermId { get; init; }
    public required string? DmsInvoice { get; init; }
    public required bool IsForeign { get; init; }
    public required int? LcTypeId { get; init; }
    public required string? LcNo { get; init; }
    public required int? PaymentTermsId { get; init; }
    public required bool IsAllowPartialShipment { get; init; }
    public required bool IsAllowTransitShipment { get; init; }
    public required string? Review { get; init; }
    public required IReadOnlyList<SaveSalesOrderLine> Lines { get; init; }
}

public sealed record SalesOrderLineDetail : SalesLineDetail
{
    [JsonConstructor]
    public SalesOrderLineDetail()
    {
    }

    [SetsRequiredMembers]
    public SalesOrderLineDetail(SalesOrderLine l)
        : base(l)
    {
        DeliveredQuantity = l.DeliveredQuantity;
        ScheduledQuantity = l.ScheduledQuantity;
        DemandQuantity = l.DemandQuantity;
        FobRate = l.FobRate;
        FreightRate = l.FreightRate;
    }

    public required decimal DeliveredQuantity { get; init; }
    public required decimal ScheduledQuantity { get; init; }
    public required decimal? DemandQuantity { get; init; }
    public required decimal FobRate { get; init; }
    public required decimal FreightRate { get; init; }
}

public sealed record OrderDelivery(int Id, string Code, DateOnly DeliveryDate, decimal TotalQuantity, decimal NetAmount, string Status, bool IsConfirmed);

public sealed record SalesOrderDetail : SalesDocumentDetail
{
    [JsonConstructor]
    public SalesOrderDetail()
    {
    }

    [SetsRequiredMembers]
    public SalesOrderDetail(SalesOrder o, SalesNames names, IReadOnlyList<OrderDelivery> deliveries)
        : base(o, names)
    {
        SalesQuotationId = o.SalesQuotationId;
        SalesQuotationName = o.SalesQuotationCode;
        DeliveredQuantity = o.DeliveredQuantity;
        AdvanceAmount = o.AdvanceAmount;
        AdjustedAmount = o.AdjustedAmount;
        AdvanceRemainingAmount = o.AdvanceRemainingAmount;
        DueLimit = o.DueLimit;
        CustomerPoAmount = o.CustomerPoAmount;
        CustomerPoRefAmount = o.CustomerPoRefAmount;
        SalesTermId = o.SalesTermId;
        SalesTermName = o.SalesTermName;
        DmsInvoice = o.DmsInvoice;
        IsForeign = o.IsForeign;
        LcTypeId = o.LcTypeId;
        LcTypeName = o.LcTypeName;
        LcNo = o.LcNo;
        PaymentTermsId = o.PaymentTermsId;
        PaymentTermsName = o.PaymentTermsName;
        IsAllowPartialShipment = o.IsAllowPartialShipment;
        IsAllowTransitShipment = o.IsAllowTransitShipment;
        Review = o.Review;
        IsCompleted = o.IsCompleted;
        Lines = [.. o.Lines.OrderBy(l => l.Sequence).Select(l => new SalesOrderLineDetail(l))];
        Deliveries = deliveries;
    }

    public required int? SalesQuotationId { get; init; }
    public required string? SalesQuotationName { get; init; }
    public required decimal DeliveredQuantity { get; init; }
    public required decimal AdvanceAmount { get; init; }
    public required decimal AdjustedAmount { get; init; }
    public required decimal AdvanceRemainingAmount { get; init; }
    public required decimal DueLimit { get; init; }
    public required decimal CustomerPoAmount { get; init; }
    public required decimal CustomerPoRefAmount { get; init; }
    public required int? SalesTermId { get; init; }
    public required string? SalesTermName { get; init; }
    public required string? DmsInvoice { get; init; }
    public required bool IsForeign { get; init; }
    public required int? LcTypeId { get; init; }
    public required string? LcTypeName { get; init; }
    public required string? LcNo { get; init; }
    public required int? PaymentTermsId { get; init; }
    public required string? PaymentTermsName { get; init; }
    public required bool IsAllowPartialShipment { get; init; }
    public required bool IsAllowTransitShipment { get; init; }
    public required string? Review { get; init; }
    public required bool IsCompleted { get; init; }
    public required IReadOnlyList<SalesOrderLineDetail> Lines { get; init; }
    public required IReadOnlyList<OrderDelivery> Deliveries { get; init; }
}

internal sealed class SaveSalesOrderRequestValidator : AbstractValidator<SaveSalesOrderRequest>
{
    public SaveSalesOrderRequestValidator()
    {
        SalesValidation.AddRules(this, x => x.Lines.Count);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            SalesValidation.AddLineRules(line);
            line.RuleFor(l => l.DemandQuantity).GreaterThanOrEqualTo(0).When(l => l.DemandQuantity is not null);
            line.RuleFor(l => l.FobRate).GreaterThanOrEqualTo(0).When(l => l.FobRate is not null);
            line.RuleFor(l => l.FreightRate).GreaterThanOrEqualTo(0).When(l => l.FreightRate is not null);
        });
    }
}
