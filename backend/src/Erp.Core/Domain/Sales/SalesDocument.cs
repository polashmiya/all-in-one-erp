using Erp.Core.Domain.Approvals;

namespace Erp.Core.Domain.Sales;

public abstract class SalesDocument : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = "";

    public Guid? OfficeId { get; set; }

    public Guid WarehouseId { get; set; }

    public int? SalesForceId { get; set; }

    public string? SalesForceName { get; set; }

    public int? PriceTypeId { get; set; }

    public string? PriceTypeName { get; set; }

    public string? ProjectName { get; set; }

    public int? ReferredByPartnerId { get; set; }

    public string? ReferredByPartnerName { get; set; }

    public string? CustomerPo { get; set; }

    public DateOnly? CustomerPoDate { get; set; }

    public int? PaymentTypeId { get; set; }

    public string? PaymentTypeName { get; set; }

    public int PaymentTermDays { get; set; }

    public int? PaymentMethodId { get; set; }

    public int? BankAccountId { get; set; }

    public string? BankAccountName { get; set; }

    public string? InstrumentNo { get; set; }

    public DateOnly? InstrumentDate { get; set; }

    public string? ShippingContactPerson { get; set; }

    public int? ShippingAddressId { get; set; }

    public string? ShippingAddressName { get; set; }

    public bool IsVatInclusive { get; set; }

    public bool IsGrossDiscount { get; set; }

    public decimal HeaderDiscountPercentage { get; set; }

    public decimal HeaderDiscount { get; set; }

    public decimal TotalLineDiscount { get; set; }

    public decimal NetDiscount { get; set; }

    public int? SpecialDiscountId { get; set; }

    public decimal SpecialDiscountAmount { get; set; }

    public decimal OthersCost { get; set; }

    public int? OtherChargeAccountId { get; set; }

    public decimal ExtraCharge { get; set; }

    public int? ExtraChargeAccountId { get; set; }

    public string? DeductionLabel { get; set; }

    public decimal DeductionValue { get; set; }

    public int? FreeTypeId { get; set; }

    public string? FreeTypeName { get; set; }

    public int? CurrencyId { get; set; }

    public string? CurrencyName { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal ItemTotalAmount { get; set; }

    public decimal DiscountItemTotal { get; set; }

    public decimal OfferItemTotal { get; set; }

    public decimal TotalSd { get; set; }

    public decimal TotalVat { get; set; }

    public decimal TotalAit { get; set; }

    public decimal NetAmount { get; set; }

    public decimal ReceiveAmount { get; set; }

    public decimal PendingAmount { get; set; }

    public bool IsAdvanceAdjust { get; set; }

    public int? WalletId { get; set; }

    public decimal SupplierContributionAmountPerInvoice { get; set; }

    public int? CustomOfferId { get; set; }

    public string? CustomOfferName { get; set; }

    public string? Attachment { get; set; }

    public string? Remarks { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string? CustomData { get; set; }
}
