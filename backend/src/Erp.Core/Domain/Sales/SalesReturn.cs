using Erp.Core.Domain.Approvals;

namespace Erp.Core.Domain.Sales;

public class SalesReturn : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly ReturnDate { get; set; }

    public int DeliveryId { get; set; }

    public string DeliveryCode { get; set; } = "";

    public int? OrderId { get; set; }

    public string? OrderCode { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = "";

    public string? Phone { get; set; }

    public Guid? OfficeId { get; set; }

    public Guid WarehouseId { get; set; }

    public int? SalesForceId { get; set; }

    public string? SalesForceName { get; set; }

    public int? ReturnTypeId { get; set; }

    public string? ReturnTypeName { get; set; }

    public int? ReturnReasonId { get; set; }

    public string? ReturnReasonName { get; set; }

    public int? ReceivedById { get; set; }

    public string? ReceivedByName { get; set; }

    public string? ReturnRequestNo { get; set; }

    public string? ReturnRefNo { get; set; }

    public DateOnly? ReturnRefDate { get; set; }

    public decimal ReturnRefAmount { get; set; }

    public string? CreditNoteNo { get; set; }

    public DateOnly? CreditNoteDate { get; set; }

    public string? ChallanNo { get; set; }

    public string? VatChallanNo { get; set; }

    public int? DeliveryMethodId { get; set; }

    public string? DeliveryMethodName { get; set; }

    public string? PickupAddress { get; set; }

    public bool IsExchange { get; set; }

    public bool IsVatInclusive { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal ItemTotalAmount { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal TotalSd { get; set; }

    public decimal TotalVat { get; set; }

    public decimal TotalAit { get; set; }

    public decimal SpecialDiscountAmount { get; set; }

    public decimal ChargeRefundAmount { get; set; }

    public string? DeductionLabel { get; set; }

    public decimal DeductionValue { get; set; }

    public decimal NetAmount { get; set; }

    public decimal DueAdjusted { get; set; }

    public decimal RefundAmount { get; set; }

    public decimal AdvanceAmount { get; set; }

    public int? PaymentMethodId { get; set; }

    public int? BankAccountId { get; set; }

    public string? BankAccountName { get; set; }

    public string? InstrumentNo { get; set; }

    public DateOnly? InstrumentDate { get; set; }

    public decimal TotalCost { get; set; }

    public decimal LossAmount { get; set; }

    public string? Remarks { get; set; }

    public string? Narration { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string? CustomData { get; set; }

    public List<SalesReturnLine> Lines { get; } = [];
}

public class SalesReturnLine : SalesLine
{
    public int ReturnId { get; set; }

    public int DeliveryLineId { get; set; }

    public bool IsOfferItem { get; set; }

    public bool IsDamaged { get; set; }

    public decimal CostPrice { get; set; }

    public decimal CostTotal { get; set; }

    public decimal LossAmount { get; set; }

    public string? Batch { get; set; }

    public string? ItemSerial { get; set; }

    public string? Remarks { get; set; }
}
