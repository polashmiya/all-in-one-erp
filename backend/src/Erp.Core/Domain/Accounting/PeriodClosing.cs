namespace Erp.Core.Domain.Accounting;

public class AccountingSetting : BaseEntity, IAccountOwned
{
    public const int DefaultFiscalYearStartMonth = 7;

    public Guid AccountId { get; set; }

    public int FiscalYearStartMonth { get; set; } = DefaultFiscalYearStartMonth;
}

public class PeriodClosing : MasterRecord, IBusinessUnitOwned
{
    public Guid BusinessUnitId { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public bool IsYear { get; set; }
}
