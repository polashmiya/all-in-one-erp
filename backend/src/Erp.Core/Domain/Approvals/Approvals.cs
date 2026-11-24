namespace Erp.Core.Domain.Approvals;

public enum StepStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
}

public class ApprovalLevel : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public Guid? BusinessUnitId { get; set; }

    public required string Feature { get; set; }

    public int Level { get; set; }

    public required string Name { get; set; }

    public Guid? RoleId { get; set; }

    public Guid? UserId { get; set; }
}

public class ApprovalStep : BaseEntity, IBusinessUnitOwned
{
    public Guid AccountId { get; set; }

    public Guid BusinessUnitId { get; set; }

    public required string Feature { get; set; }

    public int DocumentId { get; set; }

    public int Level { get; set; }

    public required string Name { get; set; }

    public Guid? RoleId { get; set; }

    public Guid? UserId { get; set; }

    public StepStatus Status { get; set; } = StepStatus.Pending;

    public Guid? ActedById { get; set; }

    public DateTimeOffset? ActedAt { get; set; }

    public string? Comment { get; set; }
}
