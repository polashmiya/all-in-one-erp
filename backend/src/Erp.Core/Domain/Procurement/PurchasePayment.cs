using Erp.Core.Domain.Approvals;

namespace Erp.Core.Domain.Procurement;

public class PurchasePayment : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly PaymentDate { get; set; }

    public int SupplierId { get; set; }

    public bool IsAdvance { get; set; }

    public int? PaymentMethodId { get; set; }

    public int? BankAccountId { get; set; }

    public string? PayeeName { get; set; }

    public string? BankAccountNo { get; set; }

    public string? InstrumentNo { get; set; }

    public DateOnly? InstrumentDate { get; set; }

    public string? Remarks { get; set; }

    public decimal PaymentAmount { get; set; }

    public decimal Vds { get; set; }

    public decimal Tds { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal AdvanceAdjusted { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public string? CustomData { get; set; }

    public List<PurchasePaymentLine> Lines { get; } = [];
}

public class PurchasePaymentLine
{
    public int Id { get; set; }

    public int PurchasePaymentId { get; set; }

    public int Sequence { get; set; }

    public int PurchaseReceiveId { get; set; }

    public decimal PaymentAmount { get; set; }

    public decimal Vds { get; set; }

    public decimal Tds { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal AdvanceAdjusted { get; set; }
}
