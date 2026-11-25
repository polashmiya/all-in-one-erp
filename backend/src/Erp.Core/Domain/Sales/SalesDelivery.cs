namespace Erp.Core.Domain.Sales;

public enum DeliveryType
{
    Order = 1,
    Direct = 2,
}

public class SalesDelivery : SalesDocument
{
    public DeliveryType Type { get; set; }

    public int? OrderId { get; set; }

    public string? OrderCode { get; set; }

    public string? Phone { get; set; }

    public string? ChallanNo { get; set; }

    public string? VatChallanNo { get; set; }

    public string? BillNo { get; set; }

    public string? RefInvoice { get; set; }

    public string? AnonymousAddress { get; set; }

    public string? Narration { get; set; }

    public int? DeliveryMethodId { get; set; }

    public string? DeliveryMethodName { get; set; }

    public int? CourierOrEmployeeId { get; set; }

    public string? CourierOrEmployeeName { get; set; }

    public DateOnly? SubmissionDate { get; set; }

    public DateOnly? AlternativeDeliveryDate { get; set; }

    public decimal AdvanceBalanceAdjust { get; set; }

    public decimal CustomerNetAmount { get; set; }

    public decimal ReturnAmount { get; set; }

    public decimal InterestRate { get; set; }

    public decimal NetAmountWithInterest { get; set; }

    public int TotalNoOfInstallment { get; set; }

    public DateOnly? InstallmentStartDate { get; set; }

    public int? InstallmentTypeId { get; set; }

    public string? InstallmentTypeName { get; set; }

    public decimal AmountPerInstallment { get; set; }

    public decimal CommissionPercentage { get; set; }

    public decimal EarlyPaymentDiscountAmount { get; set; }

    public bool IsPosSales { get; set; }

    public bool IsExportSales { get; set; }

    public bool IsServiceRequest { get; set; }

    public bool IsBillCreated { get; set; }

    public bool IsComplete { get; set; }

    public bool IsConfirmed { get; set; }

    public Guid? ConfirmedById { get; set; }

    public string? ConfirmedByName { get; set; }

    public DateTime? ConfirmedDate { get; set; }

    public DateOnly? ActualDeliveryDate { get; set; }

    public string? GrnNo { get; set; }

    public DateOnly? GrnDate { get; set; }

    public decimal? GrnAmount { get; set; }

    public DateOnly? GrnCollectionDate { get; set; }

    public int? CollectedById { get; set; }

    public string? CollectedByName { get; set; }

    public string? ConfirmRemarks { get; set; }

    public string? ConfirmAttachment { get; set; }

    public string? DueInvoiceRemarks { get; set; }

    public int? DueInvoicePendingReasonId { get; set; }

    public string? DueInvoicePendingReason { get; set; }

    public int InvoicePdfCount { get; set; }

    public bool ExternalApiSync { get; set; }

    public int? SmsTransactionId { get; set; }

    public int? ProjectId { get; set; }

    public int? JobOrderId { get; set; }

    public int? ScheduleId { get; set; }

    public int? OutletId { get; set; }

    public string? OutletName { get; set; }

    public bool IsDistributorReceive { get; set; }

    public int? DistributorReceiveById { get; set; }

    public string? DistributorReceiveByName { get; set; }

    public DateOnly? DistributorReceiveDate { get; set; }

    public string? DistributorReceiveRemarks { get; set; }

    public List<SalesDeliveryLine> Lines { get; } = [];
}

public class SalesDeliveryLine : SalesDocumentLine
{
    public int DeliveryId { get; set; }

    public int? OrderId { get; set; }

    public string? OrderCode { get; set; }

    public int? OrderLineId { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal ChangeQuantity { get; set; }

    public DateOnly? WarrantyExpiredDate { get; set; }

    public string? WarrantyDescription { get; set; }

    public decimal CostPrice { get; set; }

    public decimal CostTotal { get; set; }

    public string? AnonymousAddress { get; set; }

    public string? ItemSerial { get; set; }

    public string? Batch { get; set; }

    public string? PackQuantity { get; set; }

    public decimal ConfirmationReturnQuantity { get; set; }

    public int? ExchangeRefId { get; set; }

    public decimal ExchangeQuantity { get; set; }

    public string? RejectReason { get; set; }

    public decimal SalesReturnLossAmount { get; set; }

    public int? OriginalDeliveryLineId { get; set; }
}
