namespace Erp.Core.Domain.Procurement;

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Closed = 4,
}

public interface IApprovalDocument
{
    int Id { get; }

    string Code { get; set; }

    bool IsActive { get; set; }

    ApprovalStatus Status { get; set; }

    DateTime? ReviewedAt { get; set; }

    Guid? ReviewedById { get; set; }

    string? ReviewComment { get; set; }
}
