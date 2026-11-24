using Erp.Core.Domain.Approvals;

namespace Erp.Core.Domain.Accounting;

public enum TaxType
{
    Vds = 1,
    Tds = 2,
}

public class TaxDeposit : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly DepositDate { get; set; }

    public TaxType Type { get; set; }

    public int? PaymentMethodId { get; set; }

    public int? BankAccountId { get; set; }

    public string? ChallanNo { get; set; }

    public DateOnly? ChallanDate { get; set; }

    public string? Remarks { get; set; }

    public decimal TotalAmount { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public string? CustomData { get; set; }

    public List<TaxDepositLine> Lines { get; } = [];
}

public class TaxDepositLine
{
    public int Id { get; set; }

    public int TaxDepositId { get; set; }

    public int Sequence { get; set; }

    public int PurchasePaymentId { get; set; }

    public decimal Amount { get; set; }
}
