using Erp.Core.Domain.Approvals;

namespace Erp.Core.Domain.Sales;

public class SalesCollection : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly CollectionDate { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = "";

    public bool IsAdvance { get; set; }

    public Guid? OfficeId { get; set; }

    public int? CollectedById { get; set; }

    public string? CollectedByName { get; set; }

    public string? MoneyReceiptNo { get; set; }

    public string? ReferenceNo { get; set; }

    public int? PaymentMethodId { get; set; }

    public string? PaymentMethodName { get; set; }

    public int? BankAccountId { get; set; }

    public string? BankAccountName { get; set; }

    public string? CustomerBankName { get; set; }

    public string? CustomerBankBranch { get; set; }

    public int? InstrumentTypeId { get; set; }

    public string? InstrumentTypeName { get; set; }

    public string? InstrumentNo { get; set; }

    public DateOnly? InstrumentDate { get; set; }

    public DateOnly? DepositDate { get; set; }

    public string? TransactionNo { get; set; }

    public string? VdsCertificateNo { get; set; }

    public DateOnly? VdsCertificateDate { get; set; }

    public string? AitCertificateNo { get; set; }

    public DateOnly? AitCertificateDate { get; set; }

    public int? CurrencyId { get; set; }

    public string? CurrencyName { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal CollectionAmount { get; set; }

    public decimal Vds { get; set; }

    public decimal Ait { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ExpenseAmount { get; set; }

    public decimal AdvanceAdjusted { get; set; }

    public decimal SettledAmount { get; set; }

    public string? Remarks { get; set; }

    public string? Narration { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string? CustomData { get; set; }

    public List<SalesCollectionLine> Lines { get; } = [];
}

public class SalesCollectionLine
{
    public int Id { get; set; }

    public int CollectionId { get; set; }

    public int Sequence { get; set; }

    public int DeliveryId { get; set; }

    public string DeliveryCode { get; set; } = "";

    public decimal CollectionAmount { get; set; }

    public decimal Vds { get; set; }

    public decimal Ait { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ExpenseAmount { get; set; }

    public decimal AdvanceAdjusted { get; set; }

    public decimal SettledAmount { get; set; }

    public string? Remarks { get; set; }
}
