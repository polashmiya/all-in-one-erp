using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Inventory;

namespace Erp.Core.Domain.Sales;

public enum QuotationOrderStatus
{
    Created = 0,
    NotCreated = 1,
    Rejected = 2,
}

public class SalesQuotation : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public int? QuotationTypeId { get; set; }

    public string Version { get; set; } = "";

    public DateOnly? VersionDate { get; set; }

    public Guid? OfficeId { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = "";

    public DateOnly QuotationDate { get; set; }

    public DateOnly ValidityDate { get; set; }

    public int? SalesForceId { get; set; }

    public string? SalesForceName { get; set; }

    public int? ReferenceId { get; set; }

    public string? Reference { get; set; }

    public string? BillingContactPerson { get; set; }

    public string? BillingPhoneNo { get; set; }

    public string? BillingAddress { get; set; }

    public string? ShippingContactPerson { get; set; }

    public int? ShippingAddressId { get; set; }

    public string? ShippingAddressName { get; set; }

    public bool IsVatInclusive { get; set; }

    public bool IsGrossDiscount { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal DeliveryCharge { get; set; }

    public decimal ExtraCharge { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Subjects { get; set; }

    public string? BodyNarration { get; set; }

    public string? FooterNarration { get; set; }

    public string? Narration { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public bool IsExpired { get; private set; }

    public bool IsOrdered { get; set; }

    public QuotationOrderStatus OrderStatus { get; set; } = QuotationOrderStatus.NotCreated;

    public string OrderStatusName { get; private set; } = "";

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string? CustomData { get; set; }

    public List<SalesQuotationLine> Lines { get; } = [];
}

public class SalesQuotationLine
{
    public int Id { get; set; }

    public int QuotationId { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public string? ItemCode { get; set; }

    public string ItemName { get; set; } = "";

    public string? ItemDescription { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public string UomName { get; set; } = "";

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal DiscountAmount { get; set; }

    public bool IsAmountWiseDiscount { get; set; }

    public bool IsDiscountPerQuantity { get; set; }

    public decimal SdPercentage { get; set; }

    public decimal SdAmount { get; set; }

    public decimal VatPercentage { get; set; }

    public decimal VatAmount { get; set; }

    public decimal AitPercentage { get; set; }

    public decimal AitAmount { get; set; }

    public decimal TotalValue { get; set; }

    public int WarrantyInMonth { get; set; }

    public string? DeliveryDate { get; set; }

    public decimal ApproveQuantity { get; set; }

    public decimal ApprovePrice { get; set; }

    public decimal ApproveAmount { get; set; }

    public bool IsActive { get; set; } = true;
}
