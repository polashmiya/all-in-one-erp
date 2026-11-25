namespace Erp.Core.Domain.Sales;

public class SalesOrder : SalesDocument
{
    public int? SalesQuotationId { get; set; }

    public string? SalesQuotationCode { get; set; }

    public decimal DeliveredQuantity { get; set; }

    public decimal AdvanceAmount { get; set; }

    public decimal AdjustedAmount { get; set; }

    public decimal AdvanceRemainingAmount { get; private set; }

    public decimal WalletCommission { get; set; }

    public decimal DueLimit { get; set; }

    public decimal CustomerPoAmount { get; set; }

    public decimal CustomerPoRefAmount { get; set; }

    public int? SalesTermId { get; set; }

    public string? SalesTermName { get; set; }

    public string? DmsInvoice { get; set; }

    public bool IsForeign { get; set; }

    public int? LcTypeId { get; set; }

    public string? LcTypeName { get; set; }

    public string? LcNo { get; set; }

    public int? ExportLcId { get; set; }

    public int? ProformaInvoiceId { get; set; }

    public int? PaymentTermsId { get; set; }

    public string? PaymentTermsName { get; set; }

    public bool IsAllowPartialShipment { get; set; }

    public bool IsAllowTransitShipment { get; set; }

    public bool IsPickPackConfirmed { get; set; }

    public int? ReasonId { get; set; }

    public string? Reason { get; set; }

    public string? Review { get; set; }

    public bool IsCompleted { get; set; }

    public Guid? ClosedById { get; set; }

    public string? ClosedRemarks { get; set; }

    public List<SalesOrderLine> Lines { get; } = [];
}

public class SalesOrderLine : SalesDocumentLine
{
    public int OrderId { get; set; }

    public decimal DeliveredQuantity { get; set; }

    public decimal ScheduledQuantity { get; set; }

    public decimal? DemandQuantity { get; set; }

    public decimal LineDiscountRate { get; private set; }

    public string? OfferName { get; set; }

    public decimal SellingItemQuantityForOffer { get; set; }

    public decimal SetUpFreeQuantity { get; set; }

    public decimal FobRate { get; set; }

    public decimal FreightRate { get; set; }
}
