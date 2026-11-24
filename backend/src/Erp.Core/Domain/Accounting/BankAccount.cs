namespace Erp.Core.Domain.Accounting;

public enum BankAccountType
{
    Bank = 1,
    Mobile = 2,
}

public class BankAccount : MasterRecord, IBusinessUnitOwned
{
    public Guid BusinessUnitId { get; set; }

    public required string Name { get; set; }

    public BankAccountType Type { get; set; }

    public string? BankName { get; set; }

    public string? BranchName { get; set; }

    public required string AccountNo { get; set; }

    public string? RoutingNo { get; set; }

    public int ChartOfAccountId { get; set; }

    public ChartOfAccount? ChartOfAccount { get; set; }

    public string? CustomData { get; set; }
}
