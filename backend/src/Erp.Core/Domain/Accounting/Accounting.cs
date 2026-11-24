using Erp.Core.Domain.Approvals;

namespace Erp.Core.Domain.Accounting;

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5,
}

public class ChartOfAccount : MasterRecord
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public AccountType Type { get; set; }

    public bool IsGroup { get; set; }

    public int? ParentId { get; set; }

    public ChartOfAccount? Parent { get; set; }

    public string? Description { get; set; }

    public string? CustomData { get; set; }
}

public class PostingAccount : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public required string Key { get; set; }

    public int ChartOfAccountId { get; set; }

    public ChartOfAccount? ChartOfAccount { get; set; }
}

public class Journal : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly JournalDate { get; set; }

    public required string Narration { get; set; }

    public string? Feature { get; set; }

    public int? DocumentId { get; set; }

    public string? DocumentCode { get; set; }

    public bool IsManual { get; set; }

    public decimal TotalAmount { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public string? CustomData { get; set; }

    public List<JournalLine> Lines { get; } = [];
}

public class JournalLine
{
    public int Id { get; set; }

    public int JournalId { get; set; }

    public Journal? Journal { get; set; }

    public int Sequence { get; set; }

    public int ChartOfAccountId { get; set; }

    public ChartOfAccount? ChartOfAccount { get; set; }

    public int? SupplierId { get; set; }

    public decimal Amount { get; set; }

    public string? Remarks { get; set; }
}
